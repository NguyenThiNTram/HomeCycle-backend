using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
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
        private const string DisputeMediaTargetType =
            "Dispute";
        private const string DisputeMediaFolder =
            "disputes";
        private readonly IDisputeRepository
            _disputeRepository;
        private readonly IUserRepository
            _userRepository;
        private readonly IMediaService
            _mediaService;
        private readonly IUnitOfWork
            _unitOfWork;
        private readonly IValidator<CreateDisputeRequest>
            _createValidator;
        private readonly IReadOnlyDictionary<
            DisputeTargetType,
            IDisputeTargetHandler> _targetHandlers;

        public DisputeService(
            IDisputeRepository disputeRepository,
            IUserRepository userRepository,
            IMediaService mediaService,
            IUnitOfWork unitOfWork,
            IValidator<CreateDisputeRequest>
                createValidator,
            IEnumerable<IDisputeTargetHandler>
                targetHandlers)
        {
            _disputeRepository =
                disputeRepository;
            _userRepository =
                userRepository;
            _mediaService =
                mediaService;
            _unitOfWork =
                unitOfWork;
            _createValidator =
                createValidator;
            _targetHandlers =
                targetHandlers
                    .GroupBy(x => x.TargetType)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First());
        }

        public async Task<
            Result<CreateDisputeResponse>> CreateAsync(
                Guid senderId,
                CreateDisputeRequest request,
                CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join("\n", validation.Errors.Select(e => e.ErrorMessage));
                return Result<CreateDisputeResponse>.Fail(ValidationErrors.InvalidRequest(message));
            }

            if (!_targetHandlers.TryGetValue(
                    request.TargetType,
                    out var targetHandler))
            {
                return Result<CreateDisputeResponse>.Fail(DisputeErrors.UnsupportedTarget(request.TargetType));
            }

            var now = DateTime.UtcNow;

            await _unitOfWork
                .BeginTransactionAsync(
                    cancellationToken);

            try
            {
                /*
                 * Handler chịu trách nhiệm:
                 * - lock target
                 * - authorization
                 * - business window
                 * - duplicate dispute
                 * - target user
                 * - chuyển target sang disputing
                 */
                var targetResult =
                    await targetHandler
                        .PrepareCreateAsync(
                            senderId,
                            request.TargetId,
                            request.Category,
                            now,
                            cancellationToken);

                if (!targetResult.IsSuccess ||
                    targetResult.Data == null)
                {
                    await _unitOfWork
                        .RollbackTransactionAsync(
                            cancellationToken);

                    return Result<CreateDisputeResponse>.Fail(targetResult.Error!);
                }

                var target =
                    targetResult.Data;

                var dispute =
                    new dispute
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

                await _disputeRepository
                    .AddAsync(
                        dispute,
                        cancellationToken);

                /*
                 * Evidence gắn vào DisputeId,
                 * không gắn vào OrderId.
                 */
                var mediaResult =
                    await _mediaService
                        .UploadAndSaveMediaAsync(
                            dispute.DisputeId,
                            DisputeMediaTargetType,
                            DisputeMediaFolder,
                            request.EvidenceImages,
                            cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<
                        CreateDisputeResponse>
                        .Fail(
                            mediaResult.Error!);
                }

                await _unitOfWork
                    .SaveChangesAsync(
                        cancellationToken);
                await _unitOfWork
                    .CommitTransactionAsync(
                        cancellationToken);
                return Result<
                    CreateDisputeResponse>
                    .Success(
                        new CreateDisputeResponse
                        {
                            DisputeId =
                                dispute.DisputeId,

                            Status =
                                DisputeStatus.Pending,

                            CreatedAt =
                                dispute.CreatedAt,

                            EvidenceImages =
                                mediaResult.Data
                                ?? Array.Empty<
                                    MediaResponse>()
                        });
            }
            catch
            {
                await _unitOfWork
                    .RollbackTransactionAsync(
                        cancellationToken);

                throw;
            }
        }

        public async Task<
            Result<DisputeDetailResponse>>
            GetDetailForUserAsync(
                Guid disputeId,
                Guid currentUserId,
                CancellationToken cancellationToken = default)
        {
            var dispute =
                await _disputeRepository
                    .GetByIdAsync(
                        disputeId,
                        cancellationToken);

            if (dispute == null)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        DisputeErrors.NotFound);
            }

            /*
             * Người gửi hoặc người bị khiếu nại
             * đều được xem dispute.
             */
            if (dispute.SenderId !=
                    currentUserId &&
                dispute.TargetUserId !=
                    currentUserId)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        DisputeErrors.Forbidden);
            }

            return await BuildDetailAsync(
                dispute,
                cancellationToken);
        }

        public async Task<
            Result<PagedResult<
                DisputeListItemResponse>>>
            GetAllForModeratorAsync(
                DisputeSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var paged =
                await _disputeRepository
                    .GetPagedForModeratorAsync(
                        request,
                        cancellationToken);

            return Result<
                PagedResult<
                    DisputeListItemResponse>>
                .Success(paged);
        }

        public async Task<
            Result<DisputeDetailResponse>>
            GetDetailForModeratorAsync(
                Guid disputeId,
                CancellationToken cancellationToken = default)
        {
            var dispute =
                await _disputeRepository
                    .GetByIdAsync(
                        disputeId,
                        cancellationToken);

            if (dispute == null)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        DisputeErrors.NotFound);
            }

            return await BuildDetailAsync(
                dispute,
                cancellationToken);
        }

        private async Task<
            Result<DisputeDetailResponse>>
            BuildDetailAsync(
                dispute dispute,
                CancellationToken cancellationToken)
        {
            if (!dispute.DisputeTargetType.HasValue)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        new Error(
                            "DISPUTE_TARGET_MISSING",
                            "Tranh chấp không xác định TargetType."));
            }

            var targetType =
                (DisputeTargetType)
                    dispute.DisputeTargetType.Value;

            if (!_targetHandlers.TryGetValue(
                    targetType,
                    out var handler))
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        DisputeErrors
                            .UnsupportedTarget(
                                targetType));
            }

            var sender =
                await _userRepository
                    .GetByIdAsync(
                        dispute.SenderId,
                        cancellationToken);

            if (sender == null)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        new Error(
                            "DISPUTE_SENDER_NOT_FOUND",
                            "Không tìm thấy người gửi tranh chấp."));
            }

            user? targetUser = null;

            if (dispute.TargetUserId.HasValue)
            {
                targetUser =
                    await _userRepository
                        .GetByIdAsync(
                            dispute
                                .TargetUserId.Value,
                            cancellationToken);
            }

            var targetSummaryResult =
                await handler
                    .BuildSummaryAsync(
                        dispute,
                        cancellationToken);

            if (!targetSummaryResult.IsSuccess ||
                targetSummaryResult.Data == null)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        targetSummaryResult.Error!);
            }

            var mediaResult =
                await _mediaService
                    .GetByTargetsAsync(
                        new[]
                        {
                            dispute.DisputeId
                        },
                        DisputeMediaTargetType,
                        cancellationToken);

            if (!mediaResult.IsSuccess)
            {
                return Result<
                    DisputeDetailResponse>
                    .Fail(
                        mediaResult.Error!);
            }

            IReadOnlyList<MediaResponse>
                evidences =
                    Array.Empty<MediaResponse>();

            if (mediaResult.Data != null &&
                mediaResult.Data.TryGetValue(
                    dispute.DisputeId,
                    out var foundMedias))
            {
                evidences =
                    foundMedias;
            }

            var response =
                new DisputeDetailResponse
                {
                    DisputeId =
                        dispute.DisputeId,
                    Sender =
                        ToUserSummary(sender),
                    TargetUser =
                        targetUser == null
                            ? null
                            : ToUserSummary(
                                targetUser),
                    Target =
                        targetSummaryResult.Data,
                    Category =
                        dispute.DisputeCategory
                            .HasValue
                            ? (DisputeCategory?)
                                dispute
                                    .DisputeCategory
                                    .Value
                            : null,
                    Description =
                        dispute.Description,
                    Status =
                        dispute.DisputeStatus
                            .HasValue
                            ? (DisputeStatus?)
                                dispute
                                    .DisputeStatus
                                    .Value
                            : null,
                    ModeratorId =
                        dispute.ModeratorId,
                    ModeratorNote =
                        dispute.ModeratorNote,
                    CreatedAt =
                        dispute.CreatedAt,
                    UpdatedAt =
                        dispute.UpdatedAt,
                    ResolvedAt =
                        dispute.ResolvedAt,
                    EvidenceImages =
                        evidences
                };

            return Result<
                DisputeDetailResponse>
                .Success(response);
        }

        private static
            DisputeUserSummaryDto
            ToUserSummary(user user)
        {
            return new DisputeUserSummaryDto
            {
                UserId =
                    user.UserId,
                Username =
                    user.Username,
                AvatarUrl =
                    user.AvatarUrl,
                Role =
                    user.Role
            };
        }
    }
}
