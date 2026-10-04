using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Inspections;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Application.Services.Appointments;
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

        private readonly IGhnShipmentCreationService _ghnLifecycle;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPostRepository _postRepo;
        private readonly IReviewRepository _reviewRepository;
        private readonly IOfferRepository _contentOfferRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMediaService _mediaService;
        private readonly IPaymentService _paymentService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDisputeRequest> _createValidator;
        private readonly IValidator<DisputeModeratorDecisionRequest> _moderatorDecisionValidator;
        private readonly IValidator<ResolveDisputeRequest> _resolveValidator;
        private readonly IValidator<VerifyDisputeReturnRequest> _returnVerificationValidator;
        private readonly IReadOnlyDictionary<DisputeTargetType, IDisputeTargetHandler> _targetHandlers;
        private readonly IDisputeCategoryRepository _disputeCategoryRepository;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAppointmentRealtimeService _appointmentRealtimeService;
        private readonly IFinanceRealtimeService _financeRealtimeService;
        private readonly IInspectionFormRepository _inspectionFormRepository;
        private readonly IInspectionAppointmentRepository _inspectionAppointmentRepository;
        private readonly IDisputeResponseRepository _disputeResponseRepository;
        private readonly IValidator<RespondDisputeRequest> _respondValidator;
        private readonly IDisputeTimelineBuilder _disputeTimelineBuilder;

        public DisputeService(IGhnShipmentCreationService ghnLifecycle,
            IDisputeRepository disputeRepository,
            IOrderRepository orderRepository,
            IPostRepository postRepo,
            IReviewRepository reviewRepository,
            IOfferRepository contentOfferRepository,
            IAgreementFormRepository agreementRepository,
            IBusinessProfileRepository businessProfileRepository,
            IPersonalProfileRepository personalProfileRepository,
            IUserRepository userRepository,
            IMediaService mediaService,
            IPaymentService paymentService,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IShipmentRepository shipmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateDisputeRequest> createValidator,
            IValidator<DisputeModeratorDecisionRequest> moderatorDecisionValidator,
            IValidator<ResolveDisputeRequest> resolveValidator,
            IValidator<VerifyDisputeReturnRequest> returnVerificationValidator,
            IEnumerable<IDisputeTargetHandler> targetHandlers,
            IDisputeCategoryRepository disputeCategoryRepository,
            IAuditService auditService,
            IEmailService emailService,
            IAppointmentRepository appointmentRepository,
            IAppointmentRealtimeService appointmentRealtimeService,
            IFinanceRealtimeService financeRealtimeService,
            IInspectionFormRepository inspectionFormRepository,
            IInspectionAppointmentRepository inspectionAppointmentRepository,
            IDisputeResponseRepository disputeResponseRepository,
            IValidator<RespondDisputeRequest> respondValidator,
            IDisputeTimelineBuilder disputeTimelineBuilder)
        {
            _ghnLifecycle = ghnLifecycle;
            _disputeRepository = disputeRepository;
            _orderRepository = orderRepository;
            _postRepo = postRepo;
            _reviewRepository = reviewRepository;
            _contentOfferRepository = contentOfferRepository;
            _agreementRepository = agreementRepository;
            _businessProfileRepository = businessProfileRepository;
            _personalProfileRepository = personalProfileRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _paymentService = paymentService;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _moderatorDecisionValidator = moderatorDecisionValidator;
            _resolveValidator = resolveValidator;
            _returnVerificationValidator = returnVerificationValidator;
            _targetHandlers = targetHandlers
                .GroupBy(x => x.TargetType)
                .ToDictionary(x => x.Key, x => x.First());
            _disputeCategoryRepository = disputeCategoryRepository;
            _auditService = auditService;
            _emailService = emailService;
            _appointmentRepository = appointmentRepository;
            _appointmentRealtimeService = appointmentRealtimeService;
            _financeRealtimeService = financeRealtimeService;
            _inspectionFormRepository = inspectionFormRepository;
            _inspectionAppointmentRepository = inspectionAppointmentRepository;
            _disputeResponseRepository = disputeResponseRepository;
            _respondValidator = respondValidator;
            _disputeTimelineBuilder = disputeTimelineBuilder;
        }

        public async Task<Result<DisputeDecisionResponse>> ResolveByModeratorAsync(
            Guid disputeId, Guid moderatorId, ResolveDisputeRequest request,
            CancellationToken cancellationToken = default)
        {
            var existing = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
            if (existing == null)
                return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotFound);
            if (IsContentDispute(existing))
            {
                if (request.ResolutionOutcome.HasValue &&
                    request.ResolutionOutcome != DisputeResolutionOutcome.ViolationConfirmed)
                    return Result<DisputeDecisionResponse>.Fail(ValidationErrors.InvalidRequest(
                        "Báo cáo nội dung chỉ chấp nhận kết quả ViolationConfirmed khi giải quyết."));
                return await DecideContentDisputeAsync(disputeId, moderatorId, new()
                {
                    ModeratorNote = request.ModeratorNote
                }, true, cancellationToken);
            }
            return await ResolveOrderDisputeAsync(disputeId, moderatorId, request, cancellationToken);
        }

        public async Task<Result<DisputeDecisionResponse>> RejectByModeratorAsync(
            Guid disputeId, Guid moderatorId, DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default)
        {
            var existing = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
            if (existing == null)
                return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotFound);
            return IsContentDispute(existing)
                ? await DecideContentDisputeAsync(disputeId, moderatorId, request, false, cancellationToken)
                : await RejectOrderDisputeAsync(disputeId, moderatorId, request, cancellationToken);
        }

        private static bool IsContentDispute(dispute dispute) =>
            dispute.DisputeTargetType is (int)DisputeTargetType.Post or (int)DisputeTargetType.Review;

        private async Task<Result<DisputeDecisionResponse>> DecideContentDisputeAsync(
            Guid disputeId, Guid moderatorId, DisputeModeratorDecisionRequest request,
            bool confirmed, CancellationToken ct)
        {
            var validation = await _moderatorDecisionValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Result<DisputeDecisionResponse>.Fail(ValidationErrors.InvalidRequest(
                    string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var snapshot = await _disputeRepository.GetByIdAsync(disputeId, ct);
                var initialError = ValidateModeratorDecisionState(snapshot, moderatorId);
                if (initialError != null)
                    return await RollbackContentDecisionAsync(initialError, ct);
                if (!IsContentDispute(snapshot!))
                    return await RollbackContentDecisionAsync(DisputeErrors.MissingTarget, ct);

                var targetType = (DisputeTargetType)snapshot!.DisputeTargetType!.Value;
                var targetId = targetType == DisputeTargetType.Post ? snapshot.PostId : snapshot.ReviewId;
                if (!targetId.HasValue)
                    return await RollbackContentDecisionAsync(DisputeErrors.MissingTarget, ct);

                // Content first, then dispute: the same order as creation. This serializes
                // decisions across separate reports so one item incurs at most one penalty.
                post? post = null;
                review? review = null;
                if (confirmed)
                {
                    if (targetType == DisputeTargetType.Post)
                        post = await _postRepo.GetByIdForUpdateAsync(targetId.Value, ct);
                    else
                        review = await _reviewRepository.GetByIdForUpdateAsync(targetId.Value, ct);
                    if (post == null && review == null)
                        return await RollbackContentDecisionAsync(targetType == DisputeTargetType.Post
                            ? PostErrors.NotFound : ContentDisputeErrors.ReviewNotFound, ct);
                }

                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, ct);
                var stateError = ValidateModeratorDecisionState(dispute, moderatorId);
                if (stateError != null)
                    return await RollbackContentDecisionAsync(stateError, ct);

                var previousDisputeStatus = (DisputeStatus)dispute!.DisputeStatus.Value;
                var previousResolutionOutcome = dispute.ResolutionOutcome.HasValue
                    ? (DisputeResolutionOutcome?)dispute.ResolutionOutcome.Value
                    : null;

                var now = DateTime.UtcNow;
                var penaltyApplied = 0;
                if (confirmed)
                {
                    if (post?.Status == PostStatus.Deleted || review?.ReviewStatus == (int)ReviewStatus.Removed)
                        return await RollbackContentDecisionAsync(ContentDisputeErrors.TargetUnavailable, ct);

                    var alreadyConfirmed = await _disputeRepository.HasConfirmedContentViolationAsync(
                        targetType, targetId.Value, ct);
                    var policy = await _platformPolicyProvider.GetDisputeConfigAsync(ct);
                    var penalty = alreadyConfirmed ? 0 : targetType == DisputeTargetType.Post
                        ? policy.PostViolationPenaltyPoints : policy.ReviewViolationPenaltyPoints;
                    var ownerId = post?.OwnerId ?? review!.ReviewerId;
                    var ratingPolicy = review == null
                        ? null
                        : await _platformPolicyProvider.GetRatingConfigAsync(ct);
                    // Reverse only recorded impact; legacy NULL is not inferred from stars.
                    var appliedRatingDelta = review?.AppliedReputationDelta ?? 0;
                    dispute!.TargetUserId = ownerId;

                    if (post != null)
                    {
                        // Change only moderation fields; do not overwrite stock/product data.
                        await _postRepo.UpdateStatusAsync(post.PostId, PostStatus.Suspended, ct);
                        await _contentOfferRepository.ClosePendingByPostAsync(post.PostId, OfferStatus.Closed, ct);
                    }
                    else
                    {
                        review!.ReviewStatus = (int)ReviewStatus.Hidden;
                        review.AppliedReputationDelta = 0;
                        review.UpdatedAt = now;
                        await _reviewRepository.UpdateAsync(review, ct);
                    }
                    // Rating queries below must see the Hidden status inside this transaction.
                    await _unitOfWork.SaveChangesAsync(ct);
                    var profileResult = await ApplyContentProfileChangesAsync(
                        ownerId,
                        penalty,
                        review?.RevieweeId,
                        appliedRatingDelta,
                        ratingPolicy,
                        now,
                        ct);
                    if (!profileResult.IsSuccess)
                        return await RollbackContentDecisionAsync(profileResult.Error!, ct);
                    penaltyApplied = profileResult.Data;
                }

                dispute!.DisputeStatus = (int)(confirmed ? DisputeStatus.Resolved : DisputeStatus.Rejected);
                dispute.ResolutionOutcome = (int)(confirmed
                    ? DisputeResolutionOutcome.ViolationConfirmed : DisputeResolutionOutcome.NoViolation);
                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.ResolutionSource = (int)DisputeResolutionSource.ModeratorDecision;
                dispute.UpdatedAt = now;
                dispute.ResolvedAt = now;

                var contentDecisionAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousDisputeStatus.ToString(), ((DisputeStatus)dispute.DisputeStatus.Value).ToString())
                    .Add("resolutionOutcome", previousResolutionOutcome?.ToString(), ((DisputeResolutionOutcome)dispute.ResolutionOutcome.Value).ToString());

                var contentDecisionAuditMetadata = new Dictionary<string, object?>
                {
                    ["targetType"] = targetType.ToString(),
                    ["targetId"] = targetId.Value
                };

                if (confirmed)
                {
                    contentDecisionAuditMetadata["penaltyPointsApplied"] = penaltyApplied;
                    contentDecisionAuditMetadata["targetStatus"] = targetType == DisputeTargetType.Post
                        ? PostStatus.Suspended.ToString()
                        : ReviewStatus.Hidden.ToString();
                }

                var contentDecisionAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = confirmed ? AuditActions.DisputeResolve : AuditActions.DisputeReject,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = contentDecisionAuditDiff.OldValues,
                    NewValues = contentDecisionAuditDiff.NewValues,
                    Metadata = contentDecisionAuditMetadata
                };

                await _disputeRepository.UpdateAsync(dispute, ct);

                var notifications = new List<notification>
                {
                    await _notificationService.AddPendingAsync(new CreateNotificationCommand(
                        dispute.SenderId!.Value, confirmed ? "Báo cáo đã được xử lý" : "Báo cáo bị từ chối",
                        confirmed ? $"Báo cáo của bạn được xác nhận vi phạm. {dispute.ModeratorNote}"
                            : $"Moderator đã từ chối báo cáo. {dispute.ModeratorNote}",
                        NotificationTargetType.Dispute, disputeId), ct)
                };
                if (confirmed && dispute.TargetUserId is Guid owner && owner != dispute.SenderId)
                {
                    var action = targetType == DisputeTargetType.Post ? "Bài đăng bị đình chỉ" : "Đánh giá bị ẩn";
                    notifications.Add(await _notificationService.AddPendingAsync(new CreateNotificationCommand(
                        owner, action,
                        $"{action} do vi phạm ({(DisputeCategory?)dispute.DisputeCategory}). " +
                        $"Điểm uy tín bị trừ trong lần xử lý này: {penaltyApplied}. {dispute.ModeratorNote}",
                        NotificationTargetType.Dispute, disputeId), ct));
                }
                else if (!confirmed && dispute.TargetUserId is Guid targetUserId && targetUserId != dispute.SenderId)
                {
                    var targetName = targetType == DisputeTargetType.Post ? "bài đăng" : "đánh giá";

                    notifications.Add(await _notificationService.AddPendingAsync(new CreateNotificationCommand(
                        targetUserId,
                        "Báo cáo đã được xem xét",
                        $"Moderator không xác nhận vi phạm đối với {targetName} của bạn. " +
                        $"Không có biện pháp xử lý nào được áp dụng. {dispute.ModeratorNote}",
                        NotificationTargetType.Dispute,
                        disputeId), ct));
                }

                await _auditService.EnqueueAsync(contentDecisionAuditEvent, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                foreach (var notification in notifications)
                    await _notificationService.PublishCreatedSafelyAsync(notification);

                return Result<DisputeDecisionResponse>.Success(new()
                {
                    DisputeId = disputeId, TargetType = targetType, TargetId = targetId,
                    Status = (DisputeStatus)dispute.DisputeStatus.Value,
                    ModeratorId = moderatorId, ModeratorNote = dispute.ModeratorNote,
                    ResolutionOutcome = (DisputeResolutionOutcome?)dispute.ResolutionOutcome,
                    ResolvedAt = now, PenaltyPointsApplied = penaltyApplied,
                    PostStatus = confirmed && post != null ? Domain.Enums.PostStatus.Suspended : null,
                    ReviewStatus = confirmed && review != null ? Domain.Enums.ReviewStatus.Hidden : null
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        private async Task<Result<DisputeDecisionResponse>> RollbackContentDecisionAsync(Error error, CancellationToken ct)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return Result<DisputeDecisionResponse>.Fail(error);
        }

        private async Task<Result<int>> ApplyContentProfileChangesAsync(
            Guid ownerId,
            int penalty,
            Guid? revieweeId,
            int appliedRatingDelta,
            RatingPolicyConfigDto? ratingPolicy,
            DateTime now,
            CancellationToken ct)
        {
            var applied = 0;
            // Stable profile lock order also handles two users reporting each other's reviews.
            var userIds = new[] { ownerId, revieweeId ?? ownerId }.Distinct().OrderBy(x => x);
            foreach (var userId in userIds)
            {
                var user = await _userRepository.GetByIdAsync(userId, ct);
                if (user == null)
                    return Result<int>.Fail(ProfileErrors.UserNotFound);
                if (user.Role == UserRole.Business)
                {
                    var profile = await _businessProfileRepository.GetByUserIdForUpdateAsync(userId, ct);
                    if (profile == null)
                        return Result<int>.Fail(ProfileErrors.ProfileNotFound);
                    if (userId == ownerId)
                    {
                        var next = ReputationScoreCalculator.ApplyDelta(profile.ReputationScore, -penalty);
                        applied = profile.ReputationScore - next;
                        profile.ReputationScore = next;
                    }
                    if (userId == revieweeId && ratingPolicy != null)
                    {
                        profile.ReputationScore = Math.Clamp(
                            profile.ReputationScore - appliedRatingDelta,
                            ratingPolicy.MinimumReputationScore,
                            ratingPolicy.MaximumReputationScore);
                        profile.DisplayStarRating = ReputationScoreCalculator.CalculateDisplayStarRating(
                            await _reviewRepository.GetValidReviewsByRevieweeAsync(userId, ct),
                            ratingPolicy);
                    }
                    profile.UpdatedAt = now;
                    _businessProfileRepository.Update(profile);
                }
                else if (user.Role == UserRole.Personal)
                {
                    var profile = await _personalProfileRepository.GetByUserIdForUpdateAsync(userId, ct);
                    if (profile == null)
                        return Result<int>.Fail(ProfileErrors.ProfileNotFound);
                    if (userId == ownerId)
                    {
                        var next = ReputationScoreCalculator.ApplyDelta(profile.ReputationScore, -penalty);
                        applied = profile.ReputationScore - next;
                        profile.ReputationScore = next;
                    }
                    if (userId == revieweeId && ratingPolicy != null)
                    {
                        profile.ReputationScore = Math.Clamp(
                            profile.ReputationScore - appliedRatingDelta,
                            ratingPolicy.MinimumReputationScore,
                            ratingPolicy.MaximumReputationScore);
                        profile.DisplayStarRating = ReputationScoreCalculator.CalculateDisplayStarRating(
                            await _reviewRepository.GetValidReviewsByRevieweeAsync(userId, ct),
                            ratingPolicy);
                    }
                    await _personalProfileRepository.UpdateAsync(profile, ct);
                }
                else
                    return Result<int>.Fail(ProfileErrors.ProfileNotFound);
            }
            return Result<int>.Success(applied);
        }

        // Metadata for content reports. Order categories retain their existing contextual policy.
        public Result<DisputeOptionsResponse> GetContentOptions(DisputeTargetType? targetType)
        {
            if (targetType.HasValue && targetType is not (DisputeTargetType.Post or DisputeTargetType.Review))
                return Result<DisputeOptionsResponse>.Fail(DisputeErrors.UnsupportedTarget(targetType.Value));
            var types = targetType.HasValue
                ? new[] { targetType.Value } : new[] { DisputeTargetType.Post, DisputeTargetType.Review };
            return Result<DisputeOptionsResponse>.Success(new()
            {
                TargetTypes = types.Select(type => new DisputeTargetOptionResponse
                {
                    Value = type, Name = type.ToString(),
                    DisplayName = type == DisputeTargetType.Post ? "Bài đăng" : "Đánh giá",
                    Categories = (type == DisputeTargetType.Post
                        ? PostDisputeCategoryPolicy.BuildAllowedCategories()
                        : ReviewDisputeCategoryPolicy.BuildAllowedCategories())
                        .Select(category => new DisputeCategoryOptionResponse
                        {
                            Value = category, Name = category.ToString(), DisplayName = category switch
                            {
                                DisputeCategory.MisleadingPost => "Thông tin bài đăng sai lệch",
                                DisputeCategory.ProhibitedItem => "Sản phẩm bị cấm",
                                DisputeCategory.Spam => "Nội dung rác hoặc quảng cáo lặp lại",
                                DisputeCategory.InappropriateContent => "Nội dung không phù hợp",
                                DisputeCategory.FraudOrScam => "Dấu hiệu lừa đảo",
                                DisputeCategory.AbusiveReview => "Đánh giá mang tính xúc phạm",
                                DisputeCategory.Harassment => "Quấy rối",
                                _ => "Lý do khác"
                            }
                        }).ToArray()
                }).ToArray()
            });
        }

        public async Task<Result<CreateDisputeResponse>> CreateAsync(
            Guid senderId,
            CreateDisputeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join(
                    "\n",
                    validation.Errors.Select(x => x.ErrorMessage));

                return Result<CreateDisputeResponse>.Fail(
                    ValidationErrors.InvalidRequest(message));
            }

            if (!_targetHandlers.TryGetValue(request.TargetType, out var targetHandler))
                return Result<CreateDisputeResponse>.Fail(
                    DisputeErrors.UnsupportedTarget(request.TargetType));

            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var category = await _disputeCategoryRepository.GetByIdForUpdateAsync(
                    request.DisputeCategoryId,
                    cancellationToken);

                if (category == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(
                        DisputeCategoryErrors.NotFound);
                }

                if (!category.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(
                        DisputeCategoryErrors.Inactive);
                }

                if (!category.TargetTypes.Contains(request.TargetType))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(
                        DisputeCategoryErrors.TargetNotAllowed);
                }

                var targetResult = await targetHandler.PrepareCreateAsync(
                    senderId,
                    request.TargetId,
                    category.Code,
                    now,
                    cancellationToken);

                if (!targetResult.IsSuccess || targetResult.Data == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(
                        targetResult.Error!);
                }

                var target = targetResult.Data;
                var isOrderDispute = target.TargetType == DisputeTargetType.Order;

                var source = isOrderDispute && target.OrderId.HasValue
                    ? await ResolveOrderDisputeSourceAsync(
                        target.OrderId.Value,
                        category.Code,
                        cancellationToken)
                    : (DisputeOrigin.UserReported, (Guid?)null);

                // Sự cố vận chuyển GHN không phải lỗi của bên bị khiếu nại nên gửi thẳng cho kiểm duyệt viên,
                // bỏ qua bước hai bên tự xử lý.
                var isGhnCarrierDispute =
                    isOrderDispute &&
                    OrderDisputeCategoryPolicy.IsGhnCarrierCategory(category.Code);

                var sendsDirectlyToModerator = !isOrderDispute || isGhnCarrierDispute;

                var initialStatus = sendsDirectlyToModerator
                    ? DisputeStatus.Pending
                    : DisputeStatus.AwaitingResponse;

                int? responseWindowHours = null;
                DateTime? responseDeadlineAt = null;

                if (!sendsDirectlyToModerator)
                {
                    var orderDisputePolicy =
                        await _platformPolicyProvider.GetDisputeConfigAsync(
                            cancellationToken);

                    responseWindowHours =
                        orderDisputePolicy.ResponseWindowHours;

                    responseDeadlineAt =
                        now.AddHours(responseWindowHours.Value);
                }

                var dispute = new dispute
                {
                    DisputeId = Guid.NewGuid(),
                    SenderId = senderId,
                    TargetUserId = target.TargetUserId,
                    ModeratorId = null,

                    OrderId = target.OrderId,
                    ReviewId = target.ReviewId,
                    PostId = target.PostId,
                    AppointmentId = source.Item2,

                    DisputeTargetType = (int)target.TargetType,
                    DisputeCategory = request.DisputeCategoryId,
                    Origin = (int)source.Item1,

                    Description = request.Description.Trim(),
                    DisputeStatus = (int)initialStatus,

                    ResponseDeadlineAt = responseDeadlineAt,
                    // Khiếu nại đơn hàng chỉ được kiểm duyệt viên nhận khi đã có EscalatedAt.
                    EscalatedAt = isGhnCarrierDispute ? now : null,
                    ModeratorClaimedAt = null,

                    ModeratorNote = null,
                    ResolutionOutcome = null,
                    ResolutionSource = null,

                    CreatedAt = now,
                    UpdatedAt = now,
                    ResolvedAt = null
                };

                await _disputeRepository.AddAsync(
                    dispute,
                    cancellationToken);

                var createDisputeAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.DisputeCreate,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = senderId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,

                    NewValues = new Dictionary<string, object?>
                    {
                        ["status"] = initialStatus.ToString()
                    },

                    Metadata = new Dictionary<string, object?>
                    {
                        ["targetType"] = target.TargetType.ToString(),
                        ["targetId"] = target.TargetId,
                        ["categoryCode"] = category.Code,
                        ["origin"] = source.Item1.ToString(),
                        ["responseDeadlineAt"] = responseDeadlineAt
                    }
                };

                // User-created dispute vẫn yêu cầu 2-5 ảnh theo validator.
                var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                    targetId: dispute.DisputeId,
                    targetType: MediaTargetTypes.Dispute.ToString(),
                    folderName: $"disputes/{dispute.DisputeId}",
                    files: request.EvidenceImages,
                    uploadContext: FileUploadContext.DisputeEvidence,
                    cancellationToken: cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateDisputeResponse>.Fail(
                        mediaResult.Error!);
                }

                notification? disputeNotification = null;

                if (dispute.TargetUserId.HasValue &&
                    dispute.TargetUserId.Value != dispute.SenderId)
                {
                    var notificationMessage = isGhnCarrierDispute
                        ? "Một tranh chấp về sự cố vận chuyển GHN của đơn hàng vừa được tạo và đã chuyển cho kiểm duyệt viên xử lý."
                        : isOrderDispute
                            ? $"Một tranh chấp liên quan đến đơn hàng vừa được tạo. Bạn có {responseWindowHours!.Value} giờ để chấp nhận hoặc phản hồi."
                            : "Một tranh chấp liên quan đến bạn vừa được tạo. Vui lòng kiểm tra thông tin.";

                    disputeNotification =
                        await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                dispute.TargetUserId.Value,
                                "Có tranh chấp mới",
                                notificationMessage,
                                NotificationTargetType.Dispute,
                                dispute.DisputeId),
                            cancellationToken);
                }

                var moderatorNotifications =
                    new List<notification>();

                if (sendsDirectlyToModerator)
                {
                    var createdModeratorNotifications =
                        await _notificationService.AddPendingForActiveModeratorsAsync(
                            "Có tranh chấp mới cần xử lý",
                            $"Một tranh chấp mới về {request.TargetType} vừa được gửi và đang chờ xử lý.",
                            NotificationTargetType.Dispute,
                            dispute.DisputeId,
                            cancellationToken);

                    moderatorNotifications.AddRange(
                        createdModeratorNotifications);
                }

                await _auditService.EnqueueAsync(
                    createDisputeAuditEvent,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);


                if (disputeNotification != null)
                {
                    await _notificationService.PublishCreatedSafelyAsync(
                        disputeNotification);
                }

                await SendNewDisputeEmailsSafelyAsync(
                    dispute,
                    category.Name,
                    mediaResult.Data?.Count ?? 0,
                    cancellationToken);

                await Task.WhenAll(
                    moderatorNotifications.Select(
                        _notificationService.PublishCreatedSafelyAsync));

                if (dispute.OrderId.HasValue)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        dispute.OrderId.Value,
                        dispute.UpdatedAt);
                }

                return Result<CreateDisputeResponse>.Success(
                    new CreateDisputeResponse
                    {
                        DisputeId = dispute.DisputeId,
                        Status = initialStatus,
                        CreatedAt = dispute.CreatedAt,
                        EvidenceImages =
                            mediaResult.Data ??
                            Array.Empty<MediaResponse>()
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }
        }

        private async Task SendNewDisputeEmailsSafelyAsync(dispute dispute, string categoryName, int evidenceCount, CancellationToken ct)
        {
            if (!dispute.SenderId.HasValue ||
                !dispute.TargetUserId.HasValue ||
                dispute.TargetUserId.Value == dispute.SenderId.Value)
                return;

            try
            {
                var sender = await _userRepository.GetByIdAsync(dispute.SenderId.Value, ct);
                var target = await _userRepository.GetByIdAsync(dispute.TargetUserId.Value, ct);
                if (sender == null || target == null)
                    return;

                var orderCode = "";
                var productSummary = "Không áp dụng";
                var appointmentSummary = "Chưa có lịch hẹn";
                if (dispute.OrderId.HasValue)
                {
                    var order = await _orderRepository.GetByIdAsync(dispute.OrderId.Value, ct);
                    if (order != null)
                    {
                        orderCode = order.OrderCode;
                        productSummary = $"{order.ProductName ?? "Sản phẩm"} × {order.Quantity}";
                        var appointments = await _appointmentRepository.GetAppointmentSummariesByAgreementIdAsync(order.AgreementId, ct);
                        if (appointments.Count > 0)
                            appointmentSummary = string.Join("; ", appointments.Select(x => $"{x.AppointmentType}: {x.ScheduledAt:dd/MM/yyyy HH:mm} ({x.AppointmentStatus}) – {x.Location}"));
                    }
                }

                var recipients = new[] { (User: sender, Other: target), (User: target, Other: sender) }
                    .GroupBy(x => x.User.Email, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First());
                await Task.WhenAll(recipients.Select(recipient => _emailService.SendNewDisputeEmailAsync(
                        recipient.User.Email,
                        recipient.User.Username,
                        dispute.DisputeId.ToString(),
                        ((DisputeTargetType)dispute.DisputeTargetType!.Value).ToString(),
                        categoryName,
                        dispute.CreatedAt.ToString("dd/MM/yyyy HH:mm 'UTC'"),
                        sender.Username,
                        recipient.Other.Username,
                        orderCode,
                        productSummary,
                        appointmentSummary,
                        dispute.Description ?? "Không có mô tả",
                        evidenceCount,
                        ct)));
            }
            catch
            {
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

            var isParticipant =
                dispute.SenderId == currentUserId ||
                dispute.TargetUserId == currentUserId;

            if (!isParticipant &&
                IsSystemNoShow(dispute) &&
                dispute.OrderId.HasValue)
            {
                var order = await _orderRepository.GetByIdAsync(
                    dispute.OrderId.Value,
                    cancellationToken);

                if (order != null)
                {
                    var agreement = await _agreementRepository.GetByIdAsync(
                        order.AgreementId,
                        cancellationToken);

                    isParticipant =
                        agreement != null &&
                        (
                            agreement.BuyerId == currentUserId ||
                            agreement.SellerId == currentUserId
                        );
                }
            }

            if (!isParticipant)
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
                var disputeSnapshot = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
                if (disputeSnapshot?.OrderId is Guid relatedOrderId)
                {
                    var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(relatedOrderId, cancellationToken);
                    if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                }
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


                if (!dispute.DisputeTargetType.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.MissingTarget);
                }

                var currentStatus = dispute.DisputeStatus.HasValue
                    ? (DisputeStatus?)dispute.DisputeStatus.Value
                    : null;

                if (currentStatus is not (DisputeStatus.AwaitingResponse or DisputeStatus.Pending))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CloseDisputeResponse>.Fail(DisputeErrors.CloseNotAllowed);
                }

                var closedAt = DateTime.UtcNow;
                var previousDisputeStatus = (DisputeStatus)dispute.DisputeStatus.Value;
                OrderStatus? previousOrderStatus = null;
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

                    previousOrderStatus = (OrderStatus)order.OrderStatus.Value;
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

                var closeDisputeAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousDisputeStatus.ToString(), DisputeStatus.Closed.ToString())
                    .Add("orderStatus", previousOrderStatus?.ToString(), restoredOrderStatus?.ToString());

                var closeDisputeAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.DisputeClose,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = currentUserId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = closeDisputeAuditDiff.OldValues,
                    NewValues = closeDisputeAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["targetType"] = ((DisputeTargetType)dispute.DisputeTargetType.Value).ToString(),
                        ["orderId"] = dispute.OrderId
                    }
                };

                await _disputeRepository.UpdateAsync(dispute, cancellationToken);

                notification? closedNotification = null;

                if (dispute.TargetUserId.HasValue &&
                    dispute.TargetUserId.Value != dispute.SenderId)
                {
                    closedNotification = await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            dispute.TargetUserId.Value,
                            "Tranh chấp đã được đóng",
                            "Người tạo tranh chấp đã chủ động đóng yêu cầu. Trạng thái liên quan đã được khôi phục.",
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken);
                }

                await _auditService.EnqueueAsync(closeDisputeAuditEvent, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                if (closedNotification != null)
                    await _notificationService.PublishCreatedSafelyAsync(closedNotification);

                if (dispute.OrderId.HasValue)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        dispute.OrderId.Value,
                        dispute.UpdatedAt);
                }

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
                var disputeSnapshot = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
                if (disputeSnapshot?.OrderId is Guid relatedOrderId)
                {
                    var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(relatedOrderId, cancellationToken);
                    if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                }
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

                var isOrderDispute = dispute.DisputeTargetType == (int)DisputeTargetType.Order;

                if (isOrderDispute && !dispute.EscalatedAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ClaimDisputeResponse>.Fail(DisputeErrors.ClaimNotAllowed);
                }

                var previousDisputeStatus = status.Value;
                var now = DateTime.UtcNow;

                dispute.ModeratorId = moderatorId;
                dispute.ModeratorClaimedAt ??= now;
                dispute.DisputeStatus = (int)DisputeStatus.UnderReview;
                dispute.UpdatedAt = now;

                var claimDisputeAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousDisputeStatus.ToString(), DisputeStatus.UnderReview.ToString());

                var claimDisputeAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.DisputeClaim,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = claimDisputeAuditDiff.OldValues,
                    NewValues = claimDisputeAuditDiff.NewValues
                };

                await _disputeRepository.UpdateAsync(dispute, cancellationToken);

                var claimNotifications = new List<notification>();

if (dispute.SenderId.HasValue)
{
    claimNotifications.Add(
        await _notificationService.AddPendingAsync(
            new CreateNotificationCommand(
                dispute.SenderId.Value,
                "Tranh chấp đang được xem xét",
                "Moderator đã tiếp nhận và bắt đầu xem xét tranh chấp.",
                NotificationTargetType.Dispute,
                dispute.DisputeId),
            cancellationToken));
}

if (dispute.TargetUserId.HasValue &&
    dispute.TargetUserId != dispute.SenderId)
{
    claimNotifications.Add(
        await _notificationService.AddPendingAsync(
            new CreateNotificationCommand(
                dispute.TargetUserId.Value,
                "Tranh chấp đang được xem xét",
                "Moderator đã tiếp nhận và bắt đầu xem xét tranh chấp.",
                NotificationTargetType.Dispute,
                dispute.DisputeId),
            cancellationToken));
}

                await _auditService.EnqueueAsync(claimDisputeAuditEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                foreach (var notification in claimNotifications)
                    await _notificationService.PublishCreatedSafelyAsync(notification);

                // Khiếu nại chuyển sang UnderReview làm đổi trạng thái khiếu nại trên màn đơn hàng.
                if (dispute.OrderId.HasValue)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        dispute.OrderId.Value,
                        now);
                }

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

        private async Task<Result<DisputeDecisionResponse>> ResolveOrderDisputeAsync(
            Guid disputeId, Guid moderatorId, ResolveDisputeRequest request,
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
                var disputeSnapshot = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);

                if (disputeSnapshot?.OrderId is Guid relatedOrderId)
                {
                    var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(
                        relatedOrderId, cancellationToken);

                    if (tradeSnapshot != null)
                        await _postRepo.LockAsync(
                            tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                }

                var dispute = await _disputeRepository.GetByIdForUpdateAsync(
                    disputeId, cancellationToken);

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
                    return Result<DisputeDecisionResponse>.Fail(
                        DisputeErrors.UnsupportedTarget(targetType));
                }

                if (!dispute.OrderId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.MissingTarget);
                }

                var order = await _orderRepository.GetByIdForUpdateAsync(
                    dispute.OrderId.Value, cancellationToken);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.NotFound);
                }

                var isSystemNoShowDispute = IsSystemNoShow(dispute);

                if (!isSystemNoShowDispute && order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.NotDisputing);
                }

                if (isSystemNoShowDispute &&
                    order.OrderStatus != (int)OrderStatus.Processing &&
                    order.OrderStatus != (int)OrderStatus.Completed &&
                    order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.InvalidStatus);
                }

                var agreement = await _agreementRepository.GetByIdAsync(
                    order.AgreementId, cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(AgreementErrors.NotFound);
                }

                var previousDisputeStatus = (DisputeStatus)dispute.DisputeStatus!.Value;

                var previousResolutionOutcome = dispute.ResolutionOutcome.HasValue
                    ? (DisputeResolutionOutcome?)dispute.ResolutionOutcome.Value
                    : null;

                var previousOrderStatus = (OrderStatus)order.OrderStatus!.Value;
                var previousReturnDueAt = order.ReturnDueAt;
                var resolutionAt = DateTime.UtcNow;
                var resolutionOutcome = request.ResolutionOutcome!.Value;

                var resolutionExecutionResult = await ApplyOrderResolutionCoreAsync(
                    dispute,
                    order,
                    agreement,
                    resolutionOutcome,
                    moderatorId,
                    OrderCompletionSource.ModeratorResolved,
                    applyReputationPenalty: true,
                    resolutionAt,
                    cancellationToken);

                if (!resolutionExecutionResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(
                        resolutionExecutionResult.Error!);
                }

                var resolutionExecution = resolutionExecutionResult.Data!;

                dispute.ResolutionOutcome = (int)resolutionOutcome;
                dispute.ResolutionSource = (int)DisputeResolutionSource.ModeratorDecision;
                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                dispute.ResolvedAt = resolutionAt;
                dispute.UpdatedAt = resolutionAt;

                var refundedAmount = resolutionExecution.RefundedAmount;
                var releasedAmount = resolutionExecution.ReleasedAmount;
                var buyerHasItem = resolutionExecution.BuyerHasItem;

                FinanceRealtimeChange? financeChange = null;

                if (refundedAmount > AmountEpsilon)
                {
                    financeChange = new FinanceRealtimeChange
                    {
                        EventType = FinanceEventType.OrderRefunded,
                        UserId = agreement.BuyerId,
                        AffectedUserIds = new[] { agreement.SellerId },
                        ReferenceType = ReferenceType.Order,
                        ReferenceId = order.OrderId,
                        TransactionType = TransactionType.Order_Refund,
                        OccurredAt = resolutionAt
                    };
                }
                else if (releasedAmount > AmountEpsilon)
                {
                    financeChange = new FinanceRealtimeChange
                    {
                        EventType = FinanceEventType.OrderPayoutReleased,
                        UserId = agreement.SellerId,
                        ReferenceType = ReferenceType.Order,
                        ReferenceId = order.OrderId,
                        TransactionType = TransactionType.Payout_Release,
                        OccurredAt = resolutionAt
                    };
                }

                var resolveDisputeAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousDisputeStatus.ToString(),
                        ((DisputeStatus)dispute.DisputeStatus.Value).ToString())
                    .Add(
                        "resolutionOutcome",
                        previousResolutionOutcome?.ToString(),
                        resolutionOutcome.ToString())
                    .Add(
                        "orderStatus",
                        previousOrderStatus.ToString(),
                        ((OrderStatus)order.OrderStatus.Value).ToString())
                    .Add(
                        "returnDueAt",
                        previousReturnDueAt,
                        order.ReturnDueAt);

                var resolveDisputeAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.DisputeResolve,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = resolveDisputeAuditDiff.OldValues,
                    NewValues = resolveDisputeAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["orderId"] = order.OrderId,
                        ["buyerHasItem"] = buyerHasItem,
                        ["refundedAmount"] = refundedAmount,
                        ["releasedAmount"] = releasedAmount,
                        ["resolutionSource"] = DisputeResolutionSource.ModeratorDecision.ToString()
                    }
                };

                string buyerMessage;
                string sellerMessage;

                if (resolutionOutcome == DisputeResolutionOutcome.BuyerFavored)
                {
                    buyerMessage =
                        "Tranh chấp được giải quyết có lợi cho bạn. Khoản tiền nền tảng giữ đã được hoàn lại.";

                    sellerMessage =
                        "Tranh chấp được giải quyết có lợi cho người mua. Khoản tiền nền tảng giữ đã được hoàn lại cho người mua.";
                }
                else
                {
                    buyerMessage =
                        "Tranh chấp được giải quyết có lợi cho người bán. Khoản tiền nền tảng giữ đã được giải ngân cho người bán.";

                    sellerMessage =
                        "Tranh chấp được giải quyết có lợi cho bạn. Khoản tiền nền tảng giữ đã được giải ngân vào ví của bạn.";
                }

                var decisionNotifications = new List<notification>
                {
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Kết quả tranh chấp",
                            buyerMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken),

                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Kết quả tranh chấp",
                            sellerMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken)
                };

                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);
                await _auditService.EnqueueAsync(resolveDisputeAuditEvent, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                if (financeChange != null)
                    await _financeRealtimeService.PublishUpdatedSafelyAsync(financeChange);

                if (order.OrderStatus == (int)OrderStatus.Cancelled)
                    await _ghnLifecycle.CancelForOrderSafelyAsync(order.OrderId, cancellationToken);

                foreach (var notification in decisionNotifications)
                {
                    await _notificationService.PublishCreatedSafelyAsync(
                        notification);
                }

                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId, dispute.UpdatedAt);

                var response = _mapper.Map<DisputeDecisionResponse>(dispute);

                response.OrderStatus = (OrderStatus)order.OrderStatus.Value;
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

        private async Task<Result<DisputeDecisionResponse>> RejectOrderDisputeAsync(
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
                var disputeSnapshot = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
                if (disputeSnapshot?.OrderId is Guid relatedOrderId)
                {
                    var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(relatedOrderId, cancellationToken);
                    if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                }
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

                var isSystemNoShowDispute = IsSystemNoShow(dispute);

                if (!isSystemNoShowDispute &&
                    order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(OrderErrors.NotDisputing);
                }

                var agreement = await _agreementRepository.GetByIdAsync(
                    order.AgreementId,
                    cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(AgreementErrors.NotFound);
                }

                var previousDisputeStatus = (DisputeStatus)dispute.DisputeStatus.Value;
                var previousOrderStatus = (OrderStatus)order.OrderStatus.Value;

                var rejectedAt = DateTime.UtcNow;

                var restoredOrderStatus = isSystemNoShowDispute
                    ? previousOrderStatus
                    : order.CompletedAt.HasValue
                        ? OrderStatus.Completed
                        : OrderStatus.Processing;

                if (!isSystemNoShowDispute)
                {
                    order.OrderStatus = (int)restoredOrderStatus;
                    order.ReturnDueAt = null;
                    order.UpdatedAt = rejectedAt;
                }

                dispute.DisputeStatus = (int)DisputeStatus.Rejected;
                dispute.ResolutionOutcome = null;
                dispute.ResolutionSource =
                    (int)DisputeResolutionSource.ModeratorDecision;

                dispute.ModeratorNote = request.ModeratorNote.Trim();
                dispute.ResolvedAt = rejectedAt;
                dispute.UpdatedAt = rejectedAt;

                var rejectDisputeAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousDisputeStatus.ToString(), DisputeStatus.Rejected.ToString())
                    .Add("orderStatus", previousOrderStatus.ToString(), restoredOrderStatus.ToString());

                var rejectDisputeAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.DisputeReject,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = rejectDisputeAuditDiff.OldValues,
                    NewValues = rejectDisputeAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["orderId"] = order.OrderId,
                        ["systemNoShow"] = isSystemNoShowDispute
                    }
                };

                if (!isSystemNoShowDispute)
                {
                    await _orderRepository.UpdateAsync(order, cancellationToken);
                }
   
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);

                var rejectionMessage = isSystemNoShowDispute
                    ? "Moderator đã từ chối sự cố NO_SHOW. Giao dịch tiếp tục theo trạng thái hiện tại."
                    : "Moderator đã từ chối tranh chấp. Trạng thái đơn hàng đã được khôi phục.";

                var rejectionNotifications = new List<notification>
                {
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Tranh chấp bị từ chối",
                            rejectionMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken),

                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Tranh chấp bị từ chối",
                            rejectionMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken)
                };

                await _auditService.EnqueueAsync(rejectDisputeAuditEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                foreach (var notification in rejectionNotifications)
                    await _notificationService.PublishCreatedSafelyAsync(notification);

                if (dispute.OrderId.HasValue)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        dispute.OrderId.Value,
                        dispute.UpdatedAt);
                }

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
                var disputeSnapshot = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
                if (disputeSnapshot?.OrderId is Guid relatedOrderId)
                {
                    var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(relatedOrderId, cancellationToken);
                    if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                }
                var dispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, cancellationToken);

                if (dispute == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDecisionResponse>.Fail(DisputeErrors.NotFound);
                }

                if (dispute.ModeratorId.HasValue && dispute.ModeratorId.Value != moderatorId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<DisputeDecisionResponse>.Fail(
                        DisputeErrors.NotAssignedModerator);
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

                if (!dispute.ModeratorId.HasValue)
                {
                    dispute.ModeratorId = moderatorId;
                    dispute.ModeratorClaimedAt ??= now;
                }

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

                var previousDisputeStatus = (DisputeStatus)dispute.DisputeStatus.Value;
                var previousOrderStatus = (OrderStatus)order.OrderStatus.Value;
                var previousPaymentStatus = order.PaymentStatus.HasValue
                    ? (PaymentStatus?)order.PaymentStatus.Value
                    : null;
                var previousReturnDueAt = order.ReturnDueAt;

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
                    await _postRepo.RestoreOrderQuantityAsync(order.OrderId, true, cancellationToken);
                    ApplyReturnedOrderState(order, refundedAmount, now);
                }
                else
                {
                    order.OrderStatus = (int)OrderStatus.Completed;
                    order.ReturnDueAt = null;
                    order.ReturnedAt = null;
                    order.UpdatedAt = now;
                }

                FinanceRealtimeChange? financeChange = refundedAmount > AmountEpsilon
                    ? new FinanceRealtimeChange
                    {
                        EventType = FinanceEventType.OrderRefunded,
                        UserId = agreement.BuyerId,
                        AffectedUserIds = new[] { agreement.SellerId },
                        ReferenceType = ReferenceType.Order,
                        ReferenceId = order.OrderId,
                        TransactionType = TransactionType.Order_Refund,
                        OccurredAt = now
                    }
                    : null;

                var previousNote = dispute.ModeratorNote?.Trim();
                var verificationResult = request.IsReturnCompleted ? "Hoàn thành" : "Không hoàn thành";
                var verificationNote = $"[Xác minh hoàn trả: {verificationResult}] {request.ModeratorNote.Trim()}";

                dispute.ModeratorNote = string.IsNullOrWhiteSpace(previousNote)
                    ? verificationNote
                    : $"{previousNote}\n\n{verificationNote}";
                dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                var verifyReturnAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousDisputeStatus.ToString(), DisputeStatus.Resolved.ToString())
                    .Add("orderStatus", previousOrderStatus.ToString(), ((OrderStatus)order.OrderStatus.Value).ToString())
                    .Add("paymentStatus", previousPaymentStatus?.ToString(), order.PaymentStatus.HasValue ? ((PaymentStatus)order.PaymentStatus.Value).ToString() : null)
                    .Add("returnDueAt", previousReturnDueAt, order.ReturnDueAt);

                var verifyReturnAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.DisputeReturnVerify,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = dispute.DisputeId,
                    OldValues = verifyReturnAuditDiff.OldValues,
                    NewValues = verifyReturnAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["orderId"] = order.OrderId,
                        ["returnCompleted"] = request.IsReturnCompleted
                    }
                };

                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _disputeRepository.UpdateAsync(dispute, cancellationToken);

                var buyerVerificationMessage = request.IsReturnCompleted
                    ? "Moderator đã xác nhận việc hoàn trả thành công. Khoản tiền nền tảng giữ đã được hoàn lại."
                    : "Moderator không xác nhận việc hoàn trả thành công. Đơn hàng đã được khôi phục về trạng thái hoàn thành.";

                var sellerVerificationMessage = request.IsReturnCompleted
                    ? "Moderator đã xác nhận việc hoàn trả thành công. Đơn hàng đã chuyển sang trạng thái đã trả hàng."
                    : "Moderator không xác nhận việc hoàn trả thành công. Đơn hàng đã được khôi phục về trạng thái hoàn thành.";

                var verificationNotifications = new List<notification>
                {
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Kết quả xác minh hoàn trả",
                            buyerVerificationMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken),

                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Kết quả xác minh hoàn trả",
                            sellerVerificationMessage,
                            NotificationTargetType.Dispute,
                            dispute.DisputeId),
                        cancellationToken)
                };

                await _auditService.EnqueueAsync(verifyReturnAuditEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                foreach (var notification in verificationNotifications)
                    await _notificationService.PublishCreatedSafelyAsync(notification);

                if (dispute.OrderId.HasValue)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        dispute.OrderId.Value,
                        dispute.UpdatedAt);
                }

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

        public async Task<Result<DisputeDetailResponse>> RespondAsync(
            Guid disputeId,
            Guid responderId,
            RespondDisputeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _respondValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                var message = string.Join(
                    "\n",
                    validation.Errors.Select(x => x.ErrorMessage));

                return Result<DisputeDetailResponse>.Fail(
                    ValidationErrors.InvalidRequest(message));
            }

            dispute? committedDispute = null;
            Guid? committedOrderId = null;
            DateTime? committedOrderUpdatedAt = null;
            var cancelGhnAfterCommit = false;

            FinanceRealtimeChange? financeChange = null;
            var notifications = new List<notification>();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var lockedDispute = await _disputeRepository.GetByIdForUpdateAsync(
                    disputeId,
                    cancellationToken);

                if (lockedDispute == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(DisputeErrors.NotFound);
                }

                if (lockedDispute.DisputeTargetType != (int)DisputeTargetType.Order ||
                    !lockedDispute.OrderId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(DisputeErrors.ResponseNotAllowed);
                }

                if (lockedDispute.DisputeStatus != (int)DisputeStatus.AwaitingResponse)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(DisputeErrors.ResponseNotAllowed);
                }

                var now = DateTime.UtcNow;

                if (lockedDispute.ResponseDeadlineAt.HasValue &&
                    now > lockedDispute.ResponseDeadlineAt.Value)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<DisputeDetailResponse>.Fail(
                        DisputeErrors.ResponseWindowExpired(
                            lockedDispute.ResponseDeadlineAt.Value));
                }

                var lockedOrder = await _orderRepository.GetByIdForUpdateAsync(
                    lockedDispute.OrderId.Value,
                    cancellationToken);

                if (lockedOrder == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepository.GetByIdAsync(
                    lockedOrder.AgreementId,
                    cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(AgreementErrors.NotFound);
                }

                var responderIsParticipant =
                    responderId == agreement.BuyerId ||
                    responderId == agreement.SellerId;

                if (!responderIsParticipant)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<DisputeDetailResponse>.Fail(DisputeErrors.Forbidden);
                }

                var systemNoShow = IsSystemNoShow(lockedDispute);

                if (systemNoShow)
                {
                    if (request.ResponseType != DisputeResponseType.Statement)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(DisputeErrors.InvalidResponseType);
                    }
                }
                else
                {
                    if (!lockedDispute.TargetUserId.HasValue ||
                        lockedDispute.TargetUserId.Value != responderId)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(DisputeErrors.Forbidden);
                    }

                    if (request.ResponseType is not
                        (DisputeResponseType.Accept or DisputeResponseType.Rebut))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(DisputeErrors.InvalidResponseType);
                    }
                }

                if (await _disputeResponseRepository.ExistsByResponderAsync(
                    lockedDispute.DisputeId,
                    responderId,
                    cancellationToken))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<DisputeDetailResponse>.Fail(
                        DisputeErrors.ResponseAlreadySubmitted);
                }

                var response = new dispute_response
                {
                    DisputeResponseId = Guid.NewGuid(),
                    DisputeId = lockedDispute.DisputeId,
                    ResponderId = responderId,
                    ResponseType = (int)request.ResponseType,
                    Content = string.IsNullOrWhiteSpace(request.Content)
                        ? null
                        : request.Content.Trim(),
                    CreatedAt = now
                };

                await _disputeResponseRepository.AddAsync(response, cancellationToken);

                if (request.EvidenceImages.Count > 0)
                {
                    var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                        response.DisputeResponseId,
                        MediaTargetTypes.DisputeResponse.ToString(),
                        $"dispute-responses/{response.DisputeResponseId}",
                        request.EvidenceImages,
                        FileUploadContext.DisputeEvidence,
                        cancellationToken);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(mediaResult.Error!);
                    }
                }

                var orderChanged = false;

                if (request.ResponseType == DisputeResponseType.Accept)
                {
                    var acceptedOutcomeResult = ResolveAcceptedOutcome(
                        lockedDispute,
                        agreement);

                    if (!acceptedOutcomeResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(acceptedOutcomeResult.Error!);
                    }

                    if (lockedOrder.OrderStatus != (int)OrderStatus.Disputing)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(OrderErrors.NotDisputing);
                    }

                    var acceptedOutcome = acceptedOutcomeResult.Data;

                    var executionResult = await ApplyOrderResolutionCoreAsync(
                        lockedDispute,
                        lockedOrder,
                        agreement,
                        acceptedOutcome,
                        decisionActorId: null,
                        OrderCompletionSource.MutualDisputeResolution,
                        applyReputationPenalty: false,
                        now,
                        cancellationToken);

                    if (!executionResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<DisputeDetailResponse>.Fail(executionResult.Error!);
                    }

                    var execution = executionResult.Data!;

                    lockedDispute.ResolutionOutcome = (int)acceptedOutcome;
                    lockedDispute.ResolutionSource = (int)DisputeResolutionSource.MutualAgreement;
                    lockedDispute.DisputeStatus = (int)DisputeStatus.Resolved;
                    lockedDispute.ResolvedAt = now;
                    lockedDispute.UpdatedAt = now;

                    orderChanged = true;


                    if (execution.RefundedAmount > AmountEpsilon)
                    {
                        financeChange = new FinanceRealtimeChange
                        {
                            EventType = FinanceEventType.OrderRefunded,
                            UserId = agreement.BuyerId,
                            AffectedUserIds = new[] { agreement.SellerId },
                            ReferenceType = ReferenceType.Order,
                            ReferenceId = lockedOrder.OrderId,
                            TransactionType = TransactionType.Order_Refund,
                            OccurredAt = now
                        };
                    }
                    else if (execution.ReleasedAmount > AmountEpsilon)
                    {
                        financeChange = new FinanceRealtimeChange
                        {
                            EventType = FinanceEventType.OrderPayoutReleased,
                            UserId = agreement.SellerId,
                            ReferenceType = ReferenceType.Order,
                            ReferenceId = lockedOrder.OrderId,
                            TransactionType = TransactionType.Payout_Release,
                            OccurredAt = now
                        };
                    }

                    var settlementMessage = acceptedOutcome == DisputeResolutionOutcome.BuyerFavored
                        ? "Hai bên đã thống nhất phương án có lợi cho người mua. Khoản tiền nền tảng giữ đã được hoàn lại cho người mua."
                        : "Hai bên đã thống nhất phương án có lợi cho người bán. Khoản tiền nền tảng giữ đã được giải ngân cho người bán.";

                    notifications.Add(
                        await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                agreement.BuyerId,
                                "Hai bên đã thống nhất tranh chấp",
                                settlementMessage,
                                NotificationTargetType.Dispute,
                                lockedDispute.DisputeId),
                            cancellationToken));

                    notifications.Add(
                        await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                agreement.SellerId,
                                "Hai bên đã thống nhất tranh chấp",
                                settlementMessage,
                                NotificationTargetType.Dispute,
                                lockedDispute.DisputeId),
                            cancellationToken));
                }
                else
                {
                    lockedDispute.DisputeStatus = (int)DisputeStatus.Pending;
                    lockedDispute.EscalatedAt ??= now;
                    lockedDispute.UpdatedAt = now;

                    if (lockedDispute.SenderId.HasValue)
                    {
                        notifications.Add(
                            await _notificationService.AddPendingAsync(
                                new CreateNotificationCommand(
                                    lockedDispute.SenderId!.Value,
                                    "Tranh chấp đã được phản hồi",
                                    "Bên còn lại đã phản hồi tranh chấp. Yêu cầu đã được chuyển sang Moderator.",
                                    NotificationTargetType.Dispute,
                                    lockedDispute.DisputeId),
                                cancellationToken));
                    }

                    var moderatorNotifications =
                        await _notificationService.AddPendingForActiveModeratorsAsync(
                            "Tranh chấp cần xem xét",
                            "Tranh chấp đã có phản hồi và được chuyển sang hàng chờ Moderator.",
                            NotificationTargetType.Dispute,
                            lockedDispute.DisputeId,
                            cancellationToken);

                    notifications.AddRange(moderatorNotifications);
                }

                var responseAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.DisputeRespond,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = responderId,
                    TargetType = AuditTargetTypes.Dispute,
                    TargetId = lockedDispute.DisputeId,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["responseType"] = request.ResponseType.ToString(),
                        ["resolutionSource"] = lockedDispute.ResolutionSource.HasValue
                            ? ((DisputeResolutionSource)lockedDispute.ResolutionSource.Value).ToString()
                            : null
                    }
                };

                if (orderChanged)
                    await _orderRepository.UpdateAsync(lockedOrder, cancellationToken);

                await _disputeRepository.UpdateAsync(lockedDispute, cancellationToken);
                await _auditService.EnqueueAsync(responseAuditEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                committedDispute = lockedDispute;
                committedOrderId = lockedOrder.OrderId;
                committedOrderUpdatedAt = lockedOrder.UpdatedAt;
                cancelGhnAfterCommit =
                    orderChanged &&
                    lockedOrder.OrderStatus == (int)OrderStatus.Cancelled;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            if (financeChange != null)
                await _financeRealtimeService.PublishUpdatedSafelyAsync(financeChange);

            if (cancelGhnAfterCommit && committedOrderId.HasValue)
            {
                await _ghnLifecycle.CancelForOrderSafelyAsync(
                    committedOrderId.Value,
                    cancellationToken);
            }

            foreach (var notification in notifications)
                await _notificationService.PublishCreatedSafelyAsync(notification);

            if (committedOrderId.HasValue)
            {
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    committedOrderId.Value,
                    committedOrderUpdatedAt ?? committedDispute!.UpdatedAt);
            }

            return await BuildDetailAsync(
                committedDispute!,
                responderId,
                null,
                cancellationToken);
        }

        // ========================== HELPER =============================
        
        #region HELPER


        private async Task<Result<IReadOnlyList<DisputeResponseDto>>> BuildDisputeResponsesAsync(Guid disputeId, CancellationToken cancellationToken)
        {
            var disputeResponses = await _disputeResponseRepository.GetByDisputeIdAsync(
                disputeId, cancellationToken);

            if (disputeResponses.Count == 0)
                return Result<IReadOnlyList<DisputeResponseDto>>.Success(Array.Empty<DisputeResponseDto>());

            var responseIds = disputeResponses.Select(x => x.DisputeResponseId).ToArray();

            var mediaResult = await _mediaService.GetByTargetsAsync(
                responseIds, MediaTargetTypes.DisputeResponse.ToString(), cancellationToken);

            if (!mediaResult.IsSuccess)
                return Result<IReadOnlyList<DisputeResponseDto>>.Fail(mediaResult.Error!);

            var result = new List<DisputeResponseDto>(disputeResponses.Count);

            foreach (var disputeResponse in disputeResponses)
            {
                var responder = await _userRepository.GetByIdAsync(
                    disputeResponse.ResponderId, cancellationToken);

                if (responder == null)
                    return Result<IReadOnlyList<DisputeResponseDto>>.Fail(ProfileErrors.UserNotFound);

                IReadOnlyList<MediaResponse> evidenceImages = Array.Empty<MediaResponse>();

                if (mediaResult.Data != null &&
                    mediaResult.Data.TryGetValue(disputeResponse.DisputeResponseId, out var foundMedia))
                {
                    evidenceImages = foundMedia;
                }

                result.Add(new DisputeResponseDto
                {
                    DisputeResponseId = disputeResponse.DisputeResponseId,
                    ResponseType = (DisputeResponseType)disputeResponse.ResponseType,
                    Content = disputeResponse.Content,
                    CreatedAt = disputeResponse.CreatedAt,
                    Responder = _mapper.Map<DisputeUserSummaryDto>(responder),
                    EvidenceImages = evidenceImages
                });
            }

            return Result<IReadOnlyList<DisputeResponseDto>>.Success(result);
        }

        private async Task<DisputeInspectionContextDto?> BuildInspectionContextAsync(
            dispute dispute, CancellationToken cancellationToken)
        {
            if (!dispute.AppointmentId.HasValue)
                return null;

            var inspectionAppointment = await _inspectionAppointmentRepository.GetByAppointmentIdAsync(
                dispute.AppointmentId.Value, cancellationToken);

            if (inspectionAppointment == null)
                return null;

            var inspectionForm = await _inspectionFormRepository.GetByInspectionAppointmentIdAsync(
                inspectionAppointment.InspectionAppointmentId, cancellationToken);

            if (inspectionForm == null)
                return null;

            var mediaResult = await _mediaService.GetByTargetsAsync(
                new[] { inspectionForm.InspectionFormId },
                MediaTargetTypes.Inspection.ToString(),
                cancellationToken);

            IReadOnlyList<MediaResponse> images = Array.Empty<MediaResponse>();

            if (mediaResult.IsSuccess &&
                mediaResult.Data != null &&
                mediaResult.Data.TryGetValue(inspectionForm.InspectionFormId, out var foundImages))
            {
                images = foundImages;
            }

            return new DisputeInspectionContextDto
            {
                InspectionFormId = inspectionForm.InspectionFormId,
                InspectionMode = (InspectionMode)inspectionForm.InspectionMode,
                InspectionStatus = inspectionForm.InspectionStatus.HasValue
                    ? (InspectionStatus?)inspectionForm.InspectionStatus.Value
                    : null,
                OperatingStatus = inspectionForm.OperatingStatus.HasValue
                    ? (InspectionOperatingStatus?)inspectionForm.OperatingStatus.Value
                    : null,
                AppearanceStatus = inspectionForm.AppearanceStatus.HasValue
                    ? (InspectionAppearanceStatus?)inspectionForm.AppearanceStatus.Value
                    : null,
                PartsStatus = inspectionForm.PartsStatus.HasValue
                    ? (InspectionPartsStatus?)inspectionForm.PartsStatus.Value
                    : null,
                MatchStatus = inspectionForm.MatchStatus.HasValue
                    ? (InspectionMatchStatus?)inspectionForm.MatchStatus.Value
                    : null,
                InspectorNotes = inspectionForm.InspectorNotes,
                Conclusion = inspectionForm.Conclusion.HasValue
                    ? (InspectionConclusion?)inspectionForm.Conclusion.Value
                    : null,
                SubmittedAt = inspectionForm.SubmittedAt,
                SellerDecisionAt = inspectionForm.SellerDecisionAt,
                SellerDecisionReason = inspectionForm.SellerDecisionReason,
                Images = images
            };
        }

        private async Task<DisputeAppointmentContextDto?> BuildAppointmentContextAsync(
            dispute dispute, CancellationToken cancellationToken)
        {
            if (!dispute.AppointmentId.HasValue)
                return null;

            var appointment = await _appointmentRepository.GetByIdAsync(
                dispute.AppointmentId.Value, cancellationToken);

            if (appointment == null)
                return null;

            var appointmentSummaries =
                await _appointmentRepository.GetAppointmentSummariesByAgreementIdAsync(
                    appointment.AgreementId, cancellationToken);

            var appointmentSummary = appointmentSummaries
                .FirstOrDefault(x => x.AppointmentId == appointment.AppointmentId);

            return new DisputeAppointmentContextDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentType = appointment.AppointmentType.HasValue
                    ? (AppointmentType?)appointment.AppointmentType.Value
                    : null,
                AppointmentStatus = appointment.AppointmentStatus.HasValue
                    ? (AppointmentStatus?)appointment.AppointmentStatus.Value
                    : null,
                ScheduledAt = appointmentSummary?.ScheduledAt,
                Location = appointmentSummary?.Location,
                BuyerCheckAt = appointment.BuyerCheckAt,
                SellerCheckAt = appointment.SellerCheckAt,
                LateThresholdAt = appointment.LateThresholdAt
            };
        }


        // Người mua đã cầm hàng khi: đơn đã hoàn tất, người bán đã xác nhận bàn giao trực tiếp
        // (giống cách tự hoàn tất đơn tính mốc bàn giao), hoặc GHN báo đã giao.
        private static bool BuyerHasItem(order order, shipment? shipment) =>
            order.CompletedAt.HasValue ||
            order.SellerHandoverConfirmedAt.HasValue ||
            (
                shipment?.DeliveryMethod == DeliveryMethod.GhnDelivery &&
                shipment.ShipmentStatus == ShipmentStatus.Delivered &&
                shipment.DeliveredAt.HasValue
            );

        // GHN đã lấy hàng và đang giao (gồm giao thất bại chờ giao lại). Có PickedUpAt mà trạng thái
        // còn ReadyToPick thì coi như webhook cập nhật trạng thái chưa tới.
        private static bool IsGhnParcelInTransit(shipment? shipment) =>
            shipment?.DeliveryMethod == DeliveryMethod.GhnDelivery &&
            (
                shipment.ShipmentStatus == ShipmentStatus.Delivering ||
                (
                    shipment.PickedUpAt.HasValue &&
                    shipment.ShipmentStatus == ShipmentStatus.ReadyToPick
                )
            );

        private async Task<bool> IsGhnParcelInTransitAsync(
            dispute dispute,
            CancellationToken cancellationToken)
        {
            if (!dispute.OrderId.HasValue)
                return false;

            var order = await _orderRepository.GetByIdAsync(
                dispute.OrderId.Value,
                cancellationToken);

            if (order == null)
                return false;

            var shipment = await _shipmentRepository.GetByOrderIdAsync(
                order.OrderId,
                cancellationToken);

            return !BuyerHasItem(order, shipment) && IsGhnParcelInTransit(shipment);
        }

        private async Task<bool> IsShippingFeeRefundCategoryAsync(
            dispute dispute,
            CancellationToken cancellationToken)
        {
            if (!dispute.DisputeCategory.HasValue)
                return false;

            var category = await _disputeCategoryRepository.GetByIdAsync(
                dispute.DisputeCategory.Value,
                cancellationToken);

            return OrderDisputeCategoryPolicy.RefundsShippingFee(category?.Code);
        }


        private async Task FinalizeAppointmentsAfterOrderResolutionAsync(
            Guid agreementId,
            bool buyerHasItem,
            DateTime now,
            CancellationToken ct)
        {
            if (buyerHasItem)
            {
                var completedCollection = await CollectionAppointmentCompletion.CompleteOpenAsync(
                    _appointmentRepository,
                    agreementId,
                    now,
                    ct);

                if (completedCollection != null)
                {
                    _unitOfWork.RegisterAfterCommit(() =>
                        _appointmentRealtimeService.PublishUpdatedSafelyAsync(
                            completedCollection.AppointmentId,
                            completedCollection.UpdatedAt));
                }

                return;
            }

            foreach (var appointmentType in new[] { AppointmentType.Inspection, AppointmentType.Collection })
            {
                var snapshot = await _appointmentRepository.GetByAgreementIdAndTypeAsync(
                    agreementId,
                    appointmentType,
                    ct);

                if (snapshot == null)
                    continue;

                var appointment = await _appointmentRepository.GetByIdForUpdateAsync(
                    snapshot.AppointmentId,
                    ct);

                if (appointment?.AppointmentStatus is not
                    ((int)AppointmentStatus.Scheduled or (int)AppointmentStatus.InProgress))
                {
                    continue;
                }

                appointment.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                appointment.CancelledAt = now;
                appointment.CancellationReason = "Appointment closed after final dispute resolution.";
                appointment.UpdatedAt = now;

                await _appointmentRepository.UpdateAsync(appointment, ct);

                var proposalSnapshot = await _appointmentRepository.GetPendingRescheduleProposalAsync(
                    appointment.AppointmentId,
                    ct);

                if (proposalSnapshot != null)
                {
                    var proposal = await _appointmentRepository.GetByIdForUpdateAsync(
                        proposalSnapshot.AppointmentId,
                        ct);

                    if (proposal?.AppointmentStatus == (int)AppointmentStatus.Proposed)
                    {
                        proposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                        proposal.CancelledAt = now;
                        proposal.CancellationReason = "Appointment closed after final dispute resolution.";
                        proposal.UpdatedAt = now;

                        await _appointmentRepository.UpdateAsync(proposal, ct);

                        _unitOfWork.RegisterAfterCommit(() =>
                            _appointmentRealtimeService.PublishUpdatedSafelyAsync(
                                proposal.AppointmentId,
                                proposal.UpdatedAt));
                    }
                }

                _unitOfWork.RegisterAfterCommit(() =>
                    _appointmentRealtimeService.PublishUpdatedSafelyAsync(
                        appointment.AppointmentId,
                        appointment.UpdatedAt));
            }
        }

        private async Task<Result<OrderResolutionExecution>> ApplyOrderResolutionCoreAsync(
            dispute dispute,
            order order,
            agreement_form agreement,
            DisputeResolutionOutcome outcome,
            Guid? decisionActorId,
            OrderCompletionSource completionSource,
            bool applyReputationPenalty,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByOrderIdAsync(
                order.OrderId,
                cancellationToken);

            var buyerHasItem = BuyerHasItem(order, shipment);

            if (!buyerHasItem && IsGhnParcelInTransit(shipment))
            {
                return Result<OrderResolutionExecution>.Fail(
                    DisputeErrors.GhnShipmentInTransit);
            }

            var refundedAmount = 0m;
            var releasedAmount = 0m;

            if (outcome == DisputeResolutionOutcome.BuyerFavored)
            {
                var refundShippingFee =
                    !buyerHasItem &&
                    await IsShippingFeeRefundCategoryAsync(dispute, cancellationToken);

                var refundResult = refundShippingFee
                    ? await _paymentService.RefundAllRemainingOrderHeldAmountWithShippingAsync(
                        order,
                        agreement,
                        cancellationToken)
                    : await _paymentService.RefundAllRemainingOrderHeldAmountAsync(
                        order,
                        agreement,
                        cancellationToken);

                if (!refundResult.IsSuccess)
                    return Result<OrderResolutionExecution>.Fail(refundResult.Error!);

                refundedAmount = refundResult.Data;

                if (buyerHasItem)
                {
                    if (!order.CompletedAt.HasValue)
                    {
                        order.CompletedAt = now;
                        order.CompletionSource = (int)completionSource;
                    }

                    var remainingPaid = Math.Max((order.AmountPaid ?? 0) - refundedAmount, 0);

                    order.AmountPaid = remainingPaid;
                    order.AmountRemaining = 0;
                    order.PaymentStatus = remainingPaid <= AmountEpsilon
                        ? (int)PaymentStatus.Refunded
                        : (int)PaymentStatus.PartiallyRefunded;

                    order.OrderStatus = (int)OrderStatus.Completed;
                    order.BuyerReturnConfirmedAt = null;
                    order.SellerReturnReceivedAt = null;
                    order.ReturnDueAt = null;
                    order.ReturnedAt = null;
                    order.DisputeWindowEndsAt = now;
                    order.UpdatedAt = now;
                }
                else
                {
                    await _postRepo.RestoreOrderQuantityAsync(
                        order.OrderId,
                        true,
                        cancellationToken);

                    ApplyRefundedCancellationState(
                        order,
                        refundedAmount,
                        decisionActorId,
                        "Order cancelled and platform-held funds refunded after a buyer-favored dispute.",
                        now);
                }
            }
            else
            {
                if (!order.CompletedAt.HasValue)
                {
                    order.CompletedAt = now;
                    order.CompletionSource = (int)completionSource;
                }

                order.OrderStatus = (int)OrderStatus.Completed;
                order.PaymentStatus = (int)PaymentStatus.Completed;
                order.BuyerReturnConfirmedAt = null;
                order.SellerReturnReceivedAt = null;
                order.ReturnDueAt = null;
                order.ReturnedAt = null;
                order.DisputeWindowEndsAt = now;
                order.UpdatedAt = now;

                var releaseResult = await _paymentService.ReleaseAllRemainingOrderHeldAmountAsync(
                    order,
                    agreement,
                    cancellationToken);

                if (!releaseResult.IsSuccess)
                    return Result<OrderResolutionExecution>.Fail(releaseResult.Error!);

                releasedAmount = releaseResult.Data;
            }

            await FinalizeAppointmentsAfterOrderResolutionAsync(
                order.AgreementId,
                buyerHasItem,
                now,
                cancellationToken);

            if (applyReputationPenalty)
            {
                var policy = await _platformPolicyProvider.GetDisputeConfigAsync(cancellationToken);

                var penalizedUserId = outcome == DisputeResolutionOutcome.BuyerFavored
                    ? agreement.SellerId
                    : agreement.BuyerId;

                var reputationResult = await ApplyReputationPenaltyAsync(
                    penalizedUserId,
                    policy.DisputeLossPenaltyPoints,
                    now,
                    cancellationToken);

                if (!reputationResult.IsSuccess)
                    return Result<OrderResolutionExecution>.Fail(reputationResult.Error!);
            }

            return Result<OrderResolutionExecution>.Success(
                new OrderResolutionExecution
                {
                    RefundedAmount = refundedAmount,
                    ReleasedAmount = releasedAmount,
                    BuyerHasItem = buyerHasItem
                });
        }

        private sealed class OrderResolutionExecution
        {
            public decimal RefundedAmount { get; init; }
            public decimal ReleasedAmount { get; init; }
            public bool BuyerHasItem { get; init; }
        }
        private static bool IsSystemNoShow(dispute dispute)
        {
            if (dispute.SenderId.HasValue)
                return false;

            return dispute.Origin is
                (int)DisputeOrigin.InspectionNoShow or
                (int)DisputeOrigin.CollectionNoShow;
        }

        private static Result<DisputeResolutionOutcome> ResolveAcceptedOutcome(
            dispute dispute,
            agreement_form agreement)
        {
            if (!dispute.SenderId.HasValue)
                return Result<DisputeResolutionOutcome>.Fail(DisputeErrors.InvalidOrderSender);

            if (dispute.SenderId.Value == agreement.BuyerId)
            {
                return Result<DisputeResolutionOutcome>.Success(
                    DisputeResolutionOutcome.BuyerFavored);
            }

            if (dispute.SenderId.Value == agreement.SellerId)
            {
                return Result<DisputeResolutionOutcome>.Success(
                    DisputeResolutionOutcome.SellerFavored);
            }

            return Result<DisputeResolutionOutcome>.Fail(DisputeErrors.InvalidOrderSender);
        }

        private async Task<(DisputeOrigin Origin, Guid? AppointmentId)> ResolveOrderDisputeSourceAsync(
            Guid orderId, string categoryCode, CancellationToken cancellationToken)
        {
            var inspectionForm = await _inspectionFormRepository.GetLatestByOrderIdAsync(
                orderId, cancellationToken);

            if (inspectionForm == null)
                return (DisputeOrigin.UserReported, null);

            var inspectionAppointment = await _inspectionAppointmentRepository.GetByIdAsync(
                inspectionForm.InspectionAppointmentId, cancellationToken);

            if (inspectionAppointment == null)
                return (DisputeOrigin.UserReported, null);

            if (inspectionForm.InspectionStatus == (int)InspectionStatus.Rejected)
                return (DisputeOrigin.InspectionRejected, inspectionAppointment.AppointmentId);

            if (string.Equals(categoryCode, "ITEM_MISMATCH", StringComparison.OrdinalIgnoreCase))
                return (DisputeOrigin.UserReported, inspectionAppointment.AppointmentId);

            return (DisputeOrigin.UserReported, null);
        }

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
            Guid? cancelledByUserId,
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
            order.CancelledByUserId = cancelledByUserId;
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
            dispute dispute, Guid? currentUserId, Guid? moderatorId, CancellationToken cancellationToken)
        {
            if (!dispute.DisputeTargetType.HasValue)
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.MissingTarget);

            var targetType = (DisputeTargetType)dispute.DisputeTargetType.Value;

            if (!_targetHandlers.TryGetValue(targetType, out var handler))
                return Result<DisputeDetailResponse>.Fail(DisputeErrors.UnsupportedTarget(targetType));

            user? sender = null;

            if (dispute.SenderId.HasValue)
            {
                sender = await _userRepository.GetByIdAsync(dispute.SenderId.Value, cancellationToken);

                if (sender == null)
                    return Result<DisputeDetailResponse>.Fail(DisputeErrors.SenderNotFound);
            }

            user? targetUser = null;

            if (dispute.TargetUserId.HasValue)
                targetUser = await _userRepository.GetByIdAsync(dispute.TargetUserId.Value, cancellationToken);

            var targetSummaryResult = await handler.BuildSummaryAsync(dispute, cancellationToken);

            if (!targetSummaryResult.IsSuccess || targetSummaryResult.Data == null)
                return Result<DisputeDetailResponse>.Fail(targetSummaryResult.Error!);

            var mediaResult = await _mediaService.GetByTargetsAsync(
                new[] { dispute.DisputeId }, MediaTargetTypes.Dispute.ToString(), cancellationToken);

            if (!mediaResult.IsSuccess)
                return Result<DisputeDetailResponse>.Fail(mediaResult.Error!);

            IReadOnlyList<MediaResponse> evidenceImages = Array.Empty<MediaResponse>();

            if (mediaResult.Data != null &&
                mediaResult.Data.TryGetValue(dispute.DisputeId, out var foundMedia))
            {
                evidenceImages = foundMedia;
            }

            var responsesResult = await BuildDisputeResponsesAsync(
                dispute.DisputeId, cancellationToken);

            if (!responsesResult.IsSuccess)
                return Result<DisputeDetailResponse>.Fail(responsesResult.Error!);

            var appointmentContext = await BuildAppointmentContextAsync(dispute, cancellationToken);
            var inspectionContext = await BuildInspectionContextAsync(dispute, cancellationToken);

            var disputeStatus = dispute.DisputeStatus.HasValue
                ? (DisputeStatus?)dispute.DisputeStatus.Value
                : null;

            var orderSummary = targetSummaryResult.Data.Order;

            DisputeResolutionOutcome? proposedOutcome = null;

            if (targetType == DisputeTargetType.Order &&
                dispute.SenderId.HasValue &&
                dispute.OrderId.HasValue)
            {
                var order = await _orderRepository.GetByIdAsync(dispute.OrderId.Value, cancellationToken);

                if (order != null)
                {
                    var agreement = await _agreementRepository.GetByIdAsync(
                        order.AgreementId, cancellationToken);

                    if (agreement != null)
                    {
                        if (dispute.SenderId.Value == agreement.BuyerId)
                            proposedOutcome = DisputeResolutionOutcome.BuyerFavored;
                        else if (dispute.SenderId.Value == agreement.SellerId)
                            proposedOutcome = DisputeResolutionOutcome.SellerFavored;
                    }
                }
            }

            var responseWindowOpen =
                disputeStatus == DisputeStatus.AwaitingResponse &&
                (!dispute.ResponseDeadlineAt.HasValue ||
                 DateTime.UtcNow <= dispute.ResponseDeadlineAt.Value);

            var systemNoShow = IsSystemNoShow(dispute);

            var currentUserIsTarget =
                currentUserId.HasValue &&
                dispute.TargetUserId.HasValue &&
                dispute.TargetUserId.Value == currentUserId.Value;

            var acceptBlockedByGhnInTransit =
                responseWindowOpen &&
                !systemNoShow &&
                currentUserIsTarget &&
                await IsGhnParcelInTransitAsync(dispute, cancellationToken);


            DisputeCategoryOptionDto? category = null;

            if (dispute.DisputeCategory.HasValue)
            {
                var categoryEntity = await _disputeCategoryRepository.GetByIdAsync(
                    dispute.DisputeCategory.Value, cancellationToken);

                if (categoryEntity != null)
                    category = _mapper.Map<DisputeCategoryOptionDto>(categoryEntity);
            }

            var response = new DisputeDetailResponse
            {
                DisputeId = dispute.DisputeId,
                Sender = sender == null ? null : _mapper.Map<DisputeUserSummaryDto>(sender),
                TargetUser = targetUser == null ? null : _mapper.Map<DisputeUserSummaryDto>(targetUser),
                Target = targetSummaryResult.Data,
                Category = category,
                AppointmentContext = appointmentContext,
                InspectionContext = inspectionContext,
                Description = dispute.Description,
                Status = disputeStatus,
                Origin = (DisputeOrigin)dispute.Origin,
                ProposedResolutionOutcome = proposedOutcome,
                ResolutionOutcome = dispute.ResolutionOutcome.HasValue
                    ? (DisputeResolutionOutcome?)dispute.ResolutionOutcome.Value
                    : null,
                ResolutionSource = dispute.ResolutionSource.HasValue
                    ? (DisputeResolutionSource?)dispute.ResolutionSource.Value
                    : null,
                ResponseDeadlineAt = dispute.ResponseDeadlineAt,
                EscalatedAt = dispute.EscalatedAt,
                ModeratorClaimedAt = dispute.ModeratorClaimedAt,
                ModeratorId = dispute.ModeratorId,
                ModeratorNote = dispute.ModeratorNote,
                CreatedAt = dispute.CreatedAt,
                UpdatedAt = dispute.UpdatedAt,
                ResolvedAt = dispute.ResolvedAt,
                EvidenceImages = evidenceImages,
                Responses = responsesResult.Data ?? Array.Empty<DisputeResponseDto>(),
                Actions = new DisputeActionDto
                {
                    CanCloseDispute =
                        currentUserId.HasValue &&
                        dispute.SenderId.HasValue &&
                        dispute.SenderId.Value == currentUserId.Value &&
                        disputeStatus is DisputeStatus.AwaitingResponse or DisputeStatus.Pending &&
                        !dispute.ModeratorId.HasValue,

                    CanAccept =
                        responseWindowOpen &&
                        !systemNoShow &&
                        currentUserIsTarget &&
                        !acceptBlockedByGhnInTransit,
                    CanRebut = responseWindowOpen && !systemNoShow && currentUserIsTarget,
                    CanSubmitStatement = responseWindowOpen && systemNoShow && currentUserId.HasValue,

                    CanClaimDispute =
                        moderatorId.HasValue &&
                        disputeStatus == DisputeStatus.Pending &&
                        !dispute.ModeratorId.HasValue &&
                        (targetType != DisputeTargetType.Order || dispute.EscalatedAt.HasValue),

                    CanResolveDispute =
                        moderatorId.HasValue &&
                        disputeStatus == DisputeStatus.UnderReview &&
                        dispute.ModeratorId == moderatorId,

                    CanRejectDispute =
                        moderatorId.HasValue &&
                        disputeStatus == DisputeStatus.UnderReview &&
                        dispute.ModeratorId == moderatorId,

                    CanVerifyReturn = false
                }
            };

            response.Timeline = _disputeTimelineBuilder.Build(response);

            return Result<DisputeDetailResponse>.Success(response);
        }
        #endregion
    }
}
