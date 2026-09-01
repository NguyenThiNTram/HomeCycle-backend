using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMediaService _mediaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDisputeRequest> _createValidator;
        private readonly IValidator<DisputeModeratorDecisionRequest> _moderatorDecisionValidator;
        private readonly IReadOnlyDictionary<DisputeTargetType, IDisputeTargetHandler> _targetHandlers;

        public DisputeService(
            IDisputeRepository disputeRepository,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IMediaService mediaService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateDisputeRequest> createValidator,
            IValidator<DisputeModeratorDecisionRequest> moderatorDecisionValidator,
            IEnumerable<IDisputeTargetHandler> targetHandlers)
        {
            _disputeRepository = disputeRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _moderatorDecisionValidator = moderatorDecisionValidator;
            _targetHandlers = targetHandlers
                .GroupBy(x => x.TargetType)
                .ToDictionary(x => x.Key, x => x.First());
        }

        public async Task<Result<CreateDisputeResponse>> CreateAsync(
            Guid senderId,
            CreateDisputeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
                return Result<CreateDisputeResponse>.Fail(ValidationErrors.InvalidRequest(message));
            }

            if (!_targetHandlers.TryGetValue(request.TargetType, out var targetHandler))
                return Result<CreateDisputeResponse>.Fail(DisputeErrors.UnsupportedTarget(request.TargetType));

            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var targetResult = await targetHandler.PrepareCreateAsync(
                    senderId,
                    request.TargetId,
                    request.Category,
                    now,
                    cancellationToken);

                if (!targetResult.IsSuccess || targetResult.Data == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(targetResult.Error!);
                }

                var target = targetResult.Data;

                var dispute = new dispute
                {
                    DisputeId = Guid.NewGuid(),
                    SenderId = senderId,
                    TargetUserId = target.TargetUserId,
                    ModeratorId = null,
                    OrderId = target.OrderId,
                    ReviewId = target.ReviewId,
                    DisputeTargetType = (int)target.TargetType,
                    DisputeCategory = (int)request.Category,
                    Description = request.Description.Trim(),
                    DisputeStatus = (int)DisputeStatus.Pending,
                    ModeratorNote = null,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ResolvedAt = null
                };

                await _disputeRepository.AddAsync(dispute, cancellationToken);

                // Evidence bắt buộc 2-5 ảnh theo validator và luôn gắn vào DisputeId.
                var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                    targetId: dispute.DisputeId,
                    targetType: MediaTargetTypes.Dispute.ToString(),
                    folderName: $"disputes/{dispute.DisputeId}",
                    files: request.EvidenceImages,
                    cancellationToken: cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(mediaResult.Error!);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<CreateDisputeResponse>.Success(new CreateDisputeResponse
                {
                    DisputeId = dispute.DisputeId,
                    Status = DisputeStatus.Pending,
                    CreatedAt = dispute.CreatedAt,
                    EvidenceImages = mediaResult.Data ?? Array.Empty<MediaResponse>()
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<PagedResult<DisputeListItemResponse>>> GetForUserAsync(
            Guid currentUserId,
            DisputeSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var paged = await _disputeRepository.GetPagedForUserAsync(
                currentUserId,
                request,
                cancellationToken);

            return Result<PagedResult<DisputeListItemResponse>>.Success(paged);
        }

        public async Task<Result<DisputeDetailResponse>> GetDetailForUserAsync(
            Guid disputeId,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);

            if (dispute == null)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.NotFound);

            var isSender = dispute.SenderId == currentUserId;
            var isTargetUser = dispute.TargetUserId == currentUserId;

            if (!isSender && !isTargetUser)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.Forbidden);

            return await BuildDetailAsync(dispute, currentUserId, null, cancellationToken);
        }

        public async Task<Result<CloseDisputeResponse>> CloseDisputeAsync(
            Guid disputeId,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, cancellationToken);

                if (dispute == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.NotFound);
                }

                if (dispute.SenderId != currentUserId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.Forbidden);
                }

                if (dispute.DisputeStatus == (int) DisputeStatus.UnderReview || dispute.ModeratorId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.AlreadyUnderReview);
                }

                if (dispute.DisputeStatus != (int)DisputeStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.CloseNotAllowed);
                }

                if (!dispute.DisputeTargetType.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.MissingTarget);
                }

                var closedAt = DateTime.UtcNow;
                OrderStatus? restoredOrderStatus = null;

                if (dispute.DisputeTargetType == (int)DisputeTargetType.Order)
                {
                    if (!dispute.OrderId.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CloseDisputeResponse>.Fail(OrderErrors.NotFound);
                    }

                    var order = await _orderRepository.GetByIdForUpdateAsync(
                        dispute.OrderId.Value,
                        cancellationToken);

                    if (order == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CloseDisputeResponse>.Fail(OrderErrors.NotFound);
                    }

                    if (order.OrderStatus != (int)OrderStatus.Disputing)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CloseDisputeResponse>.Fail(OrderErrors.NotDisputing);
                    }

                    restoredOrderStatus = order.CompletedAt.HasValue
                        ? OrderStatus.Completed
                        : OrderStatus.Processing;

                    order.OrderStatus = (int)restoredOrderStatus.Value;
                    order.UpdatedAt = closedAt;

                    await _orderRepository.UpdateAsync(order, cancellationToken);
                }

                // Close chỉ kết thúc lifecycle của Dispute.
                // Không set ResolvedAt vì Closed != Moderator Resolved.
                dispute.DisputeStatus = (int)DisputeStatus.Closed;
                dispute.UpdatedAt = closedAt;

                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<CloseDisputeResponse>.Success(new CloseDisputeResponse
                {
                    DisputeId = dispute.DisputeId,
                    DisputeStatus = DisputeStatus.Closed,
                    OrderId = dispute.OrderId,
                    RestoredOrderStatus = restoredOrderStatus,
                    UpdatedAt = closedAt
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<PagedResult<DisputeListItemResponse>>> GetAllForModeratorAsync(
            DisputeSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var paged = await _disputeRepository.GetPagedForModeratorAsync(request, cancellationToken);
            return Result<PagedResult<DisputeListItemResponse>>.Success(paged);
        }

        public async Task<Result<DisputeDetailResponse>> GetDetailForModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            CancellationToken cancellationToken = default)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);

            if (dispute == null)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.NotFound);

            return await BuildDetailAsync(dispute, null, moderatorId, cancellationToken);
        }


        public async Task<Result<ClaimDisputeResponse>> ClaimForModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, cancellationToken);

                if (dispute == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ClaimDisputeResponse>.Fail(DisputeErrors.NotFound);
                }

                var status = dispute.DisputeStatus.HasValue
                    ? (DisputeStatus?)dispute.DisputeStatus.Value
                    : null;

                // Retry cùng request của đúng Moderator → idempotent success.
                if (status == DisputeStatus.UnderReview && dispute.ModeratorId == moderatorId)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return Result<ClaimDisputeResponse>.Success(new ClaimDisputeResponse
                    {
                        DisputeId = dispute.DisputeId,
                        Status = DisputeStatus.UnderReview,
                        ModeratorId = moderatorId,
                        UpdatedAt = dispute.UpdatedAt
                    });
                }

                if (status == DisputeStatus.UnderReview || dispute.ModeratorId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ClaimDisputeResponse>.Fail(DisputeErrors.AlreadyClaimed);
                }

                if (status != DisputeStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ClaimDisputeResponse>.Fail(DisputeErrors.ClaimNotAllowed);
                }

                var now = DateTime.UtcNow;

                dispute.ModeratorId = moderatorId;
                dispute.DisputeStatus = (int)DisputeStatus.UnderReview;
                dispute.UpdatedAt = now;

                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<ClaimDisputeResponse>.Success(new ClaimDisputeResponse
                {
                    DisputeId = dispute.DisputeId,
                    Status = DisputeStatus.UnderReview,
                    ModeratorId = moderatorId,
                    UpdatedAt = now
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public Task<Result<DisputeDecisionResponse>> ResolveByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default)
        {
            return DecideByModeratorAsync(
                disputeId,
                moderatorId,
                request,
                DisputeStatus.Resolved,
                cancellationToken);
        }

        public Task<Result<DisputeDecisionResponse>> RejectByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default)
        {
            return DecideByModeratorAsync(
                disputeId,
                moderatorId,
                request,
                DisputeStatus.Rejected,
                cancellationToken);
        }


        // ========================== HELPER =============================
        #region HELPER
        private async Task<Result<DisputeDecisionResponse>> DecideByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            DisputeStatus finalStatus,
            CancellationToken cancellationToken)
        {
            var validation = await _moderatorDecisionValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
                return Result<DisputeDecisionResponse>.Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, cancellationToken);

                if (dispute == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotFound);
                }

                if (dispute.DisputeStatus != (int)DisputeStatus.UnderReview)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.DecisionNotAllowed);
                }

                if (!dispute.ModeratorId.HasValue || dispute.ModeratorId.Value != moderatorId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotAssignedModerator);
                }

                if (!dispute.DisputeTargetType.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.MissingTarget);
                }

                var targetType = (DisputeTargetType)dispute.DisputeTargetType.Value;

                if (targetType != DisputeTargetType.Order)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.UnsupportedTarget(targetType));
                }

                if (!dispute.OrderId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.MissingTarget);
                }

                var order = await _orderRepository.GetByIdForUpdateAsync(dispute.OrderId.Value, cancellationToken);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.NotFound);
                }

                if (order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.NotDisputing);
                }

                var now = DateTime.UtcNow;
                var resultingOrderStatus = OrderStatus.Disputing;

                if (finalStatus == DisputeStatus.Rejected)
                {
                    resultingOrderStatus = order.CompletedAt.HasValue
                        ? OrderStatus.Completed
                        : OrderStatus.Processing;

                    order.OrderStatus = (int)resultingOrderStatus;
                    order.UpdatedAt = now;

                    await _orderRepository.UpdateAsync(order, cancellationToken);
                }

                dispute.DisputeStatus = (int)finalStatus;
                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<DisputeDecisionResponse>.Success(new DisputeDecisionResponse
                {
                    DisputeId = dispute.DisputeId,
                    Status = finalStatus,
                    ModeratorId = moderatorId,
                    ModeratorNote = dispute.ModeratorNote,
                    OrderId = dispute.OrderId,
                    OrderStatus = resultingOrderStatus,
                    ResolvedAt = now
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        private async Task<Result<DisputeDetailResponse>> BuildDetailAsync(
            dispute dispute,
            Guid? currentUserId,
            Guid? moderatorId,
            CancellationToken cancellationToken)
        {
            if (!dispute.DisputeTargetType.HasValue)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.MissingTarget);

            var targetType = (DisputeTargetType)dispute.DisputeTargetType.Value;

            if (!_targetHandlers.TryGetValue(targetType, out var handler))
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.UnsupportedTarget(targetType));

            var sender = await _userRepository.GetByIdAsync(dispute.SenderId, cancellationToken);

            if (sender == null)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.SenderNotFound);

            user? targetUser = null;

            if (dispute.TargetUserId.HasValue)
                targetUser = await _userRepository.GetByIdAsync(dispute.TargetUserId.Value, cancellationToken);

            var targetSummaryResult = await handler.BuildSummaryAsync(dispute, cancellationToken);

            if (!targetSummaryResult.IsSuccess || targetSummaryResult.Data == null)
                return Result<DisputeDetailResponse>.Fail(targetSummaryResult.Error!);

            var mediaResult = await _mediaService.GetByTargetsAsync(
                new[] { dispute.DisputeId },
                MediaTargetTypes.Dispute.ToString(),
                cancellationToken);

            if (!mediaResult.IsSuccess)
                return Result<DisputeDetailResponse>.Fail(mediaResult.Error!);

            IReadOnlyList<MediaResponse> evidenceImages = Array.Empty<MediaResponse>();

            if (mediaResult.Data != null &&
                mediaResult.Data.TryGetValue(dispute.DisputeId, out var foundMedia))
            {
                evidenceImages = foundMedia;
            }

            var disputeStatus = dispute.DisputeStatus.HasValue
                ? (DisputeStatus?)dispute.DisputeStatus.Value
                : null;

            var isAssignedModerator =
                moderatorId.HasValue &&
                dispute.ModeratorId.HasValue &&
                dispute.ModeratorId.Value == moderatorId.Value;

            var response = new DisputeDetailResponse
            {
                DisputeId = dispute.DisputeId,
                Sender = _mapper.Map<DisputeUserSummaryDto>(sender),
                TargetUser = targetUser == null ? null : _mapper.Map<DisputeUserSummaryDto>(targetUser),
                Target = targetSummaryResult.Data,
                Category = dispute.DisputeCategory.HasValue
                    ? (DisputeCategory?)dispute.DisputeCategory.Value
                    : null,
                Description = dispute.Description,
                Status = disputeStatus,
                ModeratorId = dispute.ModeratorId,
                ModeratorNote = dispute.ModeratorNote,
                CreatedAt = dispute.CreatedAt,
                UpdatedAt = dispute.UpdatedAt,
                ResolvedAt = dispute.ResolvedAt,
                EvidenceImages = evidenceImages,
                Actions = new DisputeActionDto
                {
                    CanCloseDispute =
                        currentUserId.HasValue &&
                        dispute.SenderId == currentUserId.Value &&
                        disputeStatus == DisputeStatus.Pending &&
                        !dispute.ModeratorId.HasValue,

                    CanClaimDispute =
                        moderatorId.HasValue &&
                        disputeStatus == DisputeStatus.Pending &&
                        !dispute.ModeratorId.HasValue,

                    CanResolveDispute =
                        isAssignedModerator &&
                        disputeStatus == DisputeStatus.UnderReview,

                    CanRejectDispute =
                        isAssignedModerator &&
                        disputeStatus == DisputeStatus.UnderReview
                }
            };

            return Result<DisputeDetailResponse>.Success(response);
        }
        #endregion
    }
}
