using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
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
        private const decimal AmountEpsilon = 0.01m;

        private readonly IDisputeRepository _disputeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMediaService _mediaService;
        private readonly IPaymentService _paymentService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDisputeRequest> _createValidator;
        private readonly IValidator<DisputeModeratorDecisionRequest> _moderatorDecisionValidator;
        private readonly IValidator<ResolveDisputeRequest> _resolveValidator;
        private readonly IValidator<VerifyDisputeReturnRequest> _returnVerificationValidator;
        private readonly IReadOnlyDictionary<DisputeTargetType, IDisputeTargetHandler> _targetHandlers;

        public DisputeService(
            IDisputeRepository disputeRepository,
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository,
            IBusinessProfileRepository businessProfileRepository,
            IPersonalProfileRepository personalProfileRepository,
            IUserRepository userRepository,
            IMediaService mediaService,
            IPaymentService paymentService,
            IPlatformPolicyProvider platformPolicyProvider,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateDisputeRequest> createValidator,
            IValidator<DisputeModeratorDecisionRequest> moderatorDecisionValidator,
            IValidator<ResolveDisputeRequest> resolveValidator,
            IValidator<VerifyDisputeReturnRequest> returnVerificationValidator,
            IEnumerable<IDisputeTargetHandler> targetHandlers)
        {
            _disputeRepository = disputeRepository;
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
            _businessProfileRepository = businessProfileRepository;
            _personalProfileRepository = personalProfileRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _paymentService = paymentService;
            _platformPolicyProvider = platformPolicyProvider;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _moderatorDecisionValidator = moderatorDecisionValidator;
            _resolveValidator = resolveValidator;
            _returnVerificationValidator = returnVerificationValidator;
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

        public async Task<Result<DisputeDecisionResponse>> ResolveByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            ResolveDisputeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _resolveValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
                return Result<DisputeDecisionResponse>.Fail(ValidationErrors.InvalidRequest(message));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, cancellationToken);
                var stateError = ValidateModeratorDecisionState(dispute, moderatorId);

                if (stateError != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(stateError);
                }

                if (!dispute!.DisputeTargetType.HasValue)
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

                var agreement = await _agreementRepository.GetByIdAsync(order.AgreementId, cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(AgreementErrors.NotFound);
                }

                var policy = await _platformPolicyProvider.GetDisputeConfigAsync(cancellationToken);
                var now = DateTime.UtcNow;
                var wasCompleted = order.CompletedAt.HasValue;
                var refundedAmount = 0m;

                dispute.ResolutionOutcome = (int)request.ResolutionOutcome;
                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.UpdatedAt = now;

                var penalizedUserId = request.ResolutionOutcome == DisputeResolutionOutcome.BuyerFavored
                    ? agreement.SellerId
                    : agreement.BuyerId;

                if (request.ResolutionOutcome == DisputeResolutionOutcome.BuyerFavored && wasCompleted)
                {
                    dispute.DisputeStatus = (int)DisputeStatus.AwaitingReturn;
                    dispute.ResolvedAt = null;

                    order.OrderStatus = (int)OrderStatus.Disputing;
                    order.BuyerReturnConfirmedAt = null;
                    order.SellerReturnReceivedAt = null;
                    order.ReturnedAt = null;
                    order.ReturnDueAt = now.AddDays(policy.ReturnWindowDays);
                    order.UpdatedAt = now;
                }
                else
                {
                    if (!wasCompleted)
                    {
                        var refundResult = await _paymentService.RefundAllRemainingOrderHeldAmountAsync(
                            order,
                            agreement,
                            cancellationToken);

                        if (!refundResult.IsSuccess)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<DisputeDecisionResponse>.Fail(refundResult.Error!);
                        }

                        refundedAmount = refundResult.Data;

                        var cancellationReason =
                            request.ResolutionOutcome == DisputeResolutionOutcome.BuyerFavored
                                ? "Order cancelled and platform-held funds refunded after a buyer-favored dispute."
                                : "Order cancelled and platform-held funds refunded because the seller retained the item.";

                        ApplyRefundedCancellationState(
                            order,
                            refundedAmount,
                            moderatorId,
                            cancellationReason,
                            now);
                    }
                    else
                    {
                        order.OrderStatus = (int)OrderStatus.Completed;
                        order.ReturnDueAt = null;
                        order.UpdatedAt = now;
                    }

                    dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                    dispute.ResolvedAt = now;
                }

                var reputationResult = await ApplyReputationPenaltyAsync(
                    penalizedUserId,
                    policy.DisputeLossPenaltyPoints,
                    now,
                    cancellationToken);

                if (!reputationResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(reputationResult.Error!);
                }

                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<DisputeDecisionResponse>(dispute);
                response.OrderStatus = (OrderStatus)order.OrderStatus!.Value;
                response.RefundedAmount = refundedAmount;
                response.ReturnDueAt = order.ReturnDueAt;

                return Result<DisputeDecisionResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<DisputeDecisionResponse>> RejectByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default)
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
                var stateError = ValidateModeratorDecisionState(dispute, moderatorId);

                if (stateError != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(stateError);
                }

                if (!dispute!.DisputeTargetType.HasValue)
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
                var restoredOrderStatus = order.CompletedAt.HasValue
                    ? OrderStatus.Completed
                    : OrderStatus.Processing;

                order.OrderStatus = (int)restoredOrderStatus;
                order.ReturnDueAt = null;
                order.UpdatedAt = now;

                dispute.DisputeStatus = (int)DisputeStatus.Rejected;
                dispute.ResolutionOutcome = null;
                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<DisputeDecisionResponse>(dispute);
                response.OrderStatus = restoredOrderStatus;
                response.RefundedAmount = 0;
                response.ReturnDueAt = null;

                return Result<DisputeDecisionResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }


        public async Task<Result<DisputeDecisionResponse>> VerifyReturnByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            VerifyDisputeReturnRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _returnVerificationValidator.ValidateAsync(request, cancellationToken);

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

                if (!dispute.ModeratorId.HasValue || dispute.ModeratorId.Value != moderatorId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotAssignedModerator);
                }

                if (dispute.DisputeStatus != (int)DisputeStatus.AwaitingReturn ||
                    dispute.ResolutionOutcome != (int)DisputeResolutionOutcome.BuyerFavored)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.ReturnVerificationNotAllowed);
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

                if (!order.CompletedAt.HasValue || !order.BuyerReturnConfirmedAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.ReturnVerificationNotAllowed);
                }

                if (!order.ReturnDueAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.ReturnVerificationNotAllowed);
                }

                var now = DateTime.UtcNow;

                if (now < order.ReturnDueAt.Value)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(
                        DisputeErrors.ReturnVerificationNotDue(order.ReturnDueAt.Value));
                }

                var agreement = await _agreementRepository.GetByIdAsync(order.AgreementId, cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(AgreementErrors.NotFound);
                }

                var refundedAmount = 0m;

                if (request.IsReturnCompleted)
                {
                    var refundResult = await _paymentService.RefundAllRemainingOrderHeldAmountAsync(
                        order,
                        agreement,
                        cancellationToken);

                    if (!refundResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDecisionResponse>.Fail(refundResult.Error!);
                    }

                    refundedAmount = refundResult.Data;
                    ApplyReturnedOrderState(order, refundedAmount, now);
                }
                else
                {
                    order.OrderStatus = (int)OrderStatus.Completed;
                    order.ReturnDueAt = null;
                    order.ReturnedAt = null;
                    order.UpdatedAt = now;
                }

                var previousNote = dispute.ModeratorNote?.Trim();
                var verificationResult = request.IsReturnCompleted ? "Hoàn thành" : "Không hoàn thành";
                var verificationNote = $"[Xác minh hoàn trả: {verificationResult}] {request.ModeratorNote.Trim()}";

                dispute.ModeratorNote = string.IsNullOrWhiteSpace(previousNote)
                    ? verificationNote
                    : $"{previousNote}\n\n{verificationNote}";
                dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<DisputeDecisionResponse>(dispute);
                response.OrderStatus = (OrderStatus)order.OrderStatus!.Value;
                response.RefundedAmount = refundedAmount;
                response.ReturnDueAt = null;

                return Result<DisputeDecisionResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ========================== HELPER =============================
        #region HELPER

        private async Task<Result<bool>> ApplyReputationPenaltyAsync(
            Guid userId,
            int penaltyPoints,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (penaltyPoints <= 0)
                return Result<bool>.Success(true);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return Result<bool>.Fail(ProfileErrors.UserNotFound);

            if (user.Role == UserRole.Business)
            {
                var profile = await _businessProfileRepository.GetByUserIdForUpdateAsync(
                    userId,
                    cancellationToken);

                if (profile == null)
                    return Result<bool>.Fail(ProfileErrors.ProfileNotFound);

                profile.ReputationScore = ReputationScoreCalculator.ApplyDelta(
                    profile.ReputationScore,
                    -penaltyPoints);
                profile.UpdatedAt = now;

                _businessProfileRepository.Update(profile);

                return Result<bool>.Success(true);
            }

            if (user.Role == UserRole.Personal)
            {
                var profile = await _personalProfileRepository.GetByUserIdForUpdateAsync(
                    userId,
                    cancellationToken);

                if (profile == null)
                    return Result<bool>.Fail(ProfileErrors.ProfileNotFound);

                profile.ReputationScore = ReputationScoreCalculator.ApplyDelta(
                    profile.ReputationScore,
                    -penaltyPoints);

                await _personalProfileRepository.UpdateAsync(profile, cancellationToken);

                return Result<bool>.Success(true);
            }

            return Result<bool>.Fail(ProfileErrors.ProfileNotFound);
        }
        private static void ApplyReturnedOrderState(order order, decimal refundedAmount, DateTime now)
        {
            var remainingPaid = Math.Max((order.AmountPaid ?? 0) - refundedAmount, 0);

            order.AmountPaid = remainingPaid;
            order.AmountRemaining = 0;
            order.PaymentStatus = remainingPaid <= AmountEpsilon
                ? (int)PaymentStatus.Refunded
                : (int)PaymentStatus.PartiallyRefunded;
            order.OrderStatus = (int)OrderStatus.Returned;
            order.ReturnDueAt = null;
            order.ReturnedAt = now;
            order.UpdatedAt = now;
        }

        private static void ApplyRefundedCancellationState(
            order order,
            decimal refundedAmount,
            Guid moderatorId,
            string reason,
            DateTime now)
        {
            var remainingPaid = Math.Max((order.AmountPaid ?? 0) - refundedAmount, 0);

            order.AmountPaid = remainingPaid;
            order.AmountRemaining = 0;
            order.PaymentStatus = remainingPaid <= AmountEpsilon
                ? (int)PaymentStatus.Refunded
                : (int)PaymentStatus.PartiallyRefunded;
            order.OrderStatus = (int)OrderStatus.Cancelled;
            order.CancelledAt = now;
            order.CancelledByUserId = moderatorId;
            order.CancellationReason = reason;
            order.BuyerReturnConfirmedAt = null;
            order.SellerReturnReceivedAt = null;
            order.ReturnDueAt = null;
            order.ReturnedAt = null;
            order.DisputeWindowEndsAt = null;
            order.UpdatedAt = now;
        }

        private static Error? ValidateModeratorDecisionState(dispute? dispute, Guid moderatorId)
        {
            if (dispute == null)
                return DisputeErrors.NotFound;

            if (dispute.DisputeStatus != (int)DisputeStatus.UnderReview)
                return DisputeErrors.DecisionNotAllowed;

            if (!dispute.ModeratorId.HasValue || dispute.ModeratorId.Value != moderatorId)
                return DisputeErrors.NotAssignedModerator;

            return null;
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

            var orderSummary = targetSummaryResult.Data.Order;

            var canVerifyReturn =
                isAssignedModerator &&
                disputeStatus == DisputeStatus.AwaitingReturn &&
                orderSummary?.BuyerReturnConfirmedAt != null &&
                orderSummary.ReturnDueAt != null &&
                DateTime.UtcNow >= orderSummary.ReturnDueAt.Value;

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
                ResolutionOutcome = dispute.ResolutionOutcome.HasValue
                    ? (DisputeResolutionOutcome?)dispute.ResolutionOutcome.Value
                    : null,
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
                        disputeStatus == DisputeStatus.UnderReview,

                    CanVerifyReturn = canVerifyReturn
                }
            };

            return Result<DisputeDetailResponse>.Success(response);
        }
        #endregion
    }
}
