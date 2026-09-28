using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.DTOs.Responses.Notifications;
using AutoMapper;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.Entitlements;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.SubscriptionPackages;
using HomeCycle.Application.Entitlements;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.SubscriptionPackages
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private const string ExpirationWarningTitle = "Gói đăng ký sắp hết hạn";
        private const string ExpirationWarningMessage = "Gói đăng ký của bạn sẽ hết hạn trong vòng 24 giờ. Sau khi gói hết hiệu lực, bài đăng của bạn vẫn được giữ nguyên nhưng sẽ không còn được ưu tiên trong khu vực nổi bật, và các quyền lợi khác của gói sẽ kết thúc.";
        private readonly IUserSubscriptionRepository _repository;
        private readonly ISubscriptionPackageRepository _packageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly IPaymentRepository _payments;
        private readonly IWithdrawalService _withdrawals;
        private readonly IPriceSuggestionQuota _priceQuota;
        private readonly ISupplierMatchQuota _supplierQuota;
        private readonly ISupplierMatchEntitlementService _supplierMatchEntitlements;
        private readonly IEntitlementResolver _entitlements;
        private readonly FreePlanOptions _freePlan;
        private readonly INotificationService _notifications;
        private readonly IPostRepository _postRepository;
        private readonly IBusinessProfileRepository _businessProfileRepository;

        public UserSubscriptionService(
            IUserSubscriptionRepository repository,
            ISubscriptionPackageRepository packageRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IMapper mapper,
            IPaymentRepository payments,
            IWithdrawalService withdrawals,
            IPriceSuggestionQuota priceQuota,
            ISupplierMatchQuota supplierQuota,
            ISupplierMatchEntitlementService supplierMatchEntitlements,
            IEntitlementResolver entitlements,
            FreePlanOptions freePlan,
            INotificationService notifications,
            IPostRepository postRepository,
            IBusinessProfileRepository businessProfileRepository)
        {
            _postRepository = postRepository;
            _businessProfileRepository = businessProfileRepository;
            _repository = repository;
            _packageRepository = packageRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _mapper = mapper;
            _payments = payments;
            _withdrawals = withdrawals;
            _priceQuota = priceQuota;
            _supplierQuota = supplierQuota;
            _supplierMatchEntitlements = supplierMatchEntitlements;
            _entitlements = entitlements;
            _freePlan = freePlan;
            _notifications = notifications;
        }

        public async Task<Result<SubscriptionPurchaseContext>> ValidatePurchaseEligibilityAsync(
            Guid userId,
            Guid packageId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);

            if (user == null)
                return Result<SubscriptionPurchaseContext>.Fail(AuthErrors.UserNotFound);

            if (user.Status != UserStatus.Active)
                return Result<SubscriptionPurchaseContext>.Fail(UserSubscriptionErrors.UserInactive);

            var package = await _packageRepository.GetByIdForUpdateAsync(packageId, cancellationToken);

            if (package == null)
                return Result<SubscriptionPurchaseContext>.Fail(SubscriptionPackageErrors.NotFound);

            if (!package.IsActive)
                return Result<SubscriptionPurchaseContext>.Fail(SubscriptionPackageErrors.Inactive);

            if (package.TargetRole != user.Role)
                return Result<SubscriptionPurchaseContext>.Fail(UserSubscriptionErrors.RoleNotEligible);

            var configurationError = SubscriptionPackageService.ValidateVipConfiguration(package.Duration, package.Entitlements.Select(x => new PackageEntitlementRequest
            {
                Key = x.EntitlementKey,
                NumericValue = x.NumericValue,
                BooleanValue = x.BooleanValue,
                IsUnlimited = x.IsUnlimited
            }).ToList(), package.TargetRole);
            if (configurationError != null)
                return Result<SubscriptionPurchaseContext>.Fail(configurationError);

            var openSubscription = await _repository.GetOpenForUpdateAsync(userId, cancellationToken);

            if (openSubscription != null)
            {
                var isExpiredActive =
                    openSubscription.Status == (int)UserSubscriptionStatus.Active &&
                    openSubscription.ExpiresAt.HasValue &&
                    openSubscription.ExpiresAt.Value <= nowUtc;

                if (!isExpiredActive)
                    return Result<SubscriptionPurchaseContext>.Fail(UserSubscriptionErrors.OpenSubscriptionExists);

                openSubscription.Status = (int)UserSubscriptionStatus.Expired;
                await _repository.UpdateAsync(openSubscription, cancellationToken);

                // Flush ngay trong transaction để partial unique index
                // không còn nhìn row cũ là Active khi insert Pending mới.
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<SubscriptionPurchaseContext>.Success(
                new SubscriptionPurchaseContext(user, package));
        }

        public async Task<user_subscription> CreatePendingSubscriptionAsync(
            Guid userId,
            subscription_package package,
            DateTime createdAtUtc,
            CancellationToken cancellationToken = default)
        {
            var subscription = new user_subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PackageId = package.PackageId,
                PackageNameSnapshot = package.Name,
                DurationDaysSnapshot = package.Duration,
                Status = (int)UserSubscriptionStatus.Pending,
                CreatedAt = createdAtUtc
            };

            subscription.Entitlements = SnapshotEntitlements(subscription.SubscriptionId, package, createdAtUtc);
            await _repository.AddAsync(subscription, cancellationToken);
            return subscription;
        }

        public async Task<Result<user_subscription>> ActivateSubscriptionAsync(
            Guid subscriptionId,
            decimal pricePaid,
            DateTime paidAtUtc,
            AuditActorType actorType,
            AuditSource auditSource,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _repository.GetByIdForUpdateAsync(subscriptionId, cancellationToken);

            if (subscription == null)
                return Result<user_subscription>.Fail(UserSubscriptionErrors.NotFound);

            if (subscription.Status == (int)UserSubscriptionStatus.Active)
                return Result<user_subscription>.Success(subscription);

            if (subscription.Status != (int)UserSubscriptionStatus.Pending)
                return Result<user_subscription>.Fail(UserSubscriptionErrors.InvalidStatus);

            if (string.IsNullOrWhiteSpace(subscription.PackageNameSnapshot) ||
                subscription.DurationDaysSnapshot is null or < 1 or > 3650 || subscription.Entitlements.Count == 0)
                return Result<user_subscription>.Fail(UserSubscriptionErrors.InvalidSnapshot);

            subscription.PricePaid = pricePaid;
            subscription.Status = (int)UserSubscriptionStatus.Active;
            subscription.ActivatedAt = paidAtUtc;
            subscription.ExpiresAt = paidAtUtc.AddDays(subscription.DurationDaysSnapshot.Value);

            await _repository.UpdateAsync(subscription, cancellationToken);
            var notification = await _notifications.AddPendingAsync(new CreateNotificationCommand(
                subscription.UserId,
                "Gói đăng ký đã được kích hoạt",
                "Gói đăng ký của bạn đã được kích hoạt. Trong thời gian gói còn hiệu lực, bài đăng của bạn được ưu tiên xuất hiện trong khu vực nổi bật theo uy tín và thời gian đăng khi bạn đã có hồ sơ cá nhân, hoặc hồ sơ doanh nghiệp đã được phê duyệt.",
                NotificationTargetType.Subscription,
                subscription.SubscriptionId), cancellationToken);
            _unitOfWork.RegisterAfterCommit(() => _notifications.PublishCreatedSafelyAsync(notification));


            await _auditService.EnqueueAsync(
                new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.SubscriptionActivate,
                    Outcome = AuditOutcome.Success,
                    ActorType = actorType,
                    Source = auditSource,
                    UserId = actorUserId,
                    TargetType = AuditTargetTypes.UserSubscription,
                    TargetId = subscription.SubscriptionId,
                    NewValues = new Dictionary<string, object?>
                    {
                        ["status"] = UserSubscriptionStatus.Active.ToString(),
                        ["pricePaid"] = pricePaid,
                        ["activatedAt"] = paidAtUtc,
                        ["expiresAt"] = subscription.ExpiresAt
                    },
                    Metadata = new Dictionary<string, object?>
                    {
                        ["userId"] = subscription.UserId,
                        ["packageId"] = subscription.PackageId,
                        ["entitlementCount"] = subscription.Entitlements.Count
                    }
                },
                cancellationToken);

            return Result<user_subscription>.Success(subscription);
        }

        public async Task<Result<bool>> CancelPendingSubscriptionAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _repository.GetByIdForUpdateAsync(subscriptionId, cancellationToken);

            if (subscription == null)
                return Result<bool>.Fail(UserSubscriptionErrors.NotFound);

            if (subscription.Status is (int)UserSubscriptionStatus.Cancelled or (int)UserSubscriptionStatus.Expired)
                return Result<bool>.Success(true);

            if (subscription.Status != (int)UserSubscriptionStatus.Pending)
                return Result<bool>.Fail(UserSubscriptionErrors.InvalidStatus);

            subscription.Status = (int)UserSubscriptionStatus.Cancelled;
            await _repository.UpdateAsync(subscription, cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelActiveSubscriptionAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);
                var subscription = await _repository.GetByIdForUpdateAsync(subscriptionId, cancellationToken);
                if (user == null || subscription == null || subscription.UserId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Fail(UserSubscriptionErrors.NotFound);
                }
                if (subscription.Status is (int)UserSubscriptionStatus.Cancelled or (int)UserSubscriptionStatus.Expired)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<bool>.Success(true);
                }
                if (subscription.Status != (int)UserSubscriptionStatus.Active)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Fail(UserSubscriptionErrors.InvalidStatus);
                }
                subscription.Status = subscription.ExpiresAt <= DateTime.UtcNow
                    ? (int)UserSubscriptionStatus.Expired
                    : (int)UserSubscriptionStatus.Cancelled;
                await _repository.UpdateAsync(subscription, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.SubscriptionCancel,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    Source = AuditSource.HttpApi,
                    UserId = userId,
                    TargetType = AuditTargetTypes.UserSubscription,
                    TargetId = subscriptionId,
                    NewValues = new Dictionary<string, object?> { ["status"] = ((UserSubscriptionStatus)subscription.Status.Value).ToString() }
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _unitOfWork.ClearTrackedEntities();
                throw;
            }
        }

        public async Task<Result<UserSubscriptionResponseDto?>> GetCurrentAsync(
            Guid userId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _repository.GetOpenAsync(userId, nowUtc, cancellationToken);
            if (subscription == null)
                return Result<UserSubscriptionResponseDto?>.Success(null);
            var response = _mapper.Map<UserSubscriptionResponseDto>(subscription);
            var payment = await _payments.GetBySubscriptionIdAsync(subscription.SubscriptionId, cancellationToken);
            response.CheckoutAmount = payment?.Amount;
            response.CheckoutExpiresAt = payment?.ExpiredAt;
            var withdrawalQuota = await _withdrawals.GetMyWithdrawalQuotaAsync(userId, cancellationToken);
            if (!withdrawalQuota.IsSuccess)
                return Result<UserSubscriptionResponseDto?>.Fail(withdrawalQuota.Error!);
            response.WithdrawalQuota = withdrawalQuota.Data;
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user?.Role == UserRole.Personal)
            {
                response.AiDailyLimit = await _priceQuota.GetDailyLimitAsync(userId, cancellationToken);
                response.AiRemainingToday = await _priceQuota.RemainingAsync(userId, response.AiDailyLimit.Value, cancellationToken);
                response.AiResetsAt = _priceQuota.ResetsAt;
            }
            else if (user?.Role == UserRole.Business)
            {
                var limit = await _entitlements.ResolveAiDailyLimitAsync(userId, UserRole.Business, nowUtc, cancellationToken);
                var quota = await _supplierQuota.GetRemainingAsync(userId, limit, cancellationToken);
                response.AiDailyLimit = quota.Limit;
                response.AiRemainingToday = quota.Remaining;
                response.AiResetsAt = quota.ResetsAt;
            }
            return Result<UserSubscriptionResponseDto?>.Success(response);
        }

        public async Task<Result<PlanBenefitsResponseDto>> GetBenefitsAsync(
            Guid userId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result<PlanBenefitsResponseDto>.Fail(AuthErrors.UserNotFound);
            if (user.Role is not (UserRole.Personal or UserRole.Business))
                return Result<PlanBenefitsResponseDto>.Fail(
                    ValidationErrors.InvalidRequest("Role không hỗ trợ gói AI."));

            var activeSubscription = await _repository.GetActiveWithEntitlementsAsync(
                userId, nowUtc, cancellationToken);
            var package = activeSubscription == null
                ? null
                : await _packageRepository.GetByIdAsync(activeSubscription.PackageId, cancellationToken);
            var isVip = activeSubscription != null;
            var postingPriorityEnabled = (await _postRepository.GetPriorityOwnerIdsAsync(new[] { userId }, cancellationToken))
                .Contains(userId);
            var postingPriorityBlockedReason = postingPriorityEnabled
                ? null
                : await GetPostingPriorityBlockedReasonAsync(user, isVip, cancellationToken);

            if (user.Role == UserRole.Personal)
            {
                var limit = await _priceQuota.GetDailyLimitAsync(userId, cancellationToken);
                var remaining = await _priceQuota.RemainingAsync(userId, limit, cancellationToken);
                return Result<PlanBenefitsResponseDto>.Success(new PlanBenefitsResponseDto
                {
                    PostingPriorityEnabled = postingPriorityEnabled,
                    PostingPriorityBlockedReason = postingPriorityBlockedReason,
                    Tier = isVip ? "VIP" : "FREE",
                    PlanName = activeSubscription?.PackageNameSnapshot ?? package?.Name ?? _freePlan.Personal.Name,
                    Description = package?.Description ?? _freePlan.Personal.Description,
                    Role = user.Role,
                    SubscriptionId = activeSubscription?.SubscriptionId,
                    ActivatedAt = activeSubscription?.ActivatedAt,
                    ExpiresAt = activeSubscription?.ExpiresAt,
                    Ai = new PlanAiUsageResponseDto
                    {
                        Feature = "PRICE_SUGGESTION",
                        DailyLimit = limit,
                        UsedToday = Math.Max(0, limit - remaining),
                        RemainingToday = remaining,
                        ResetsAt = _priceQuota.ResetsAt
                    }
                });
            }

            var entitlement = await _supplierMatchEntitlements.GetAsync(userId, cancellationToken);
            var quota = await _supplierQuota.GetRemainingAsync(
                userId, entitlement.DailyAiRefreshLimit, cancellationToken);
            return Result<PlanBenefitsResponseDto>.Success(new PlanBenefitsResponseDto
            {
                PostingPriorityEnabled = postingPriorityEnabled,
                PostingPriorityBlockedReason = postingPriorityBlockedReason,
                Tier = entitlement.Tier.ToString().ToUpperInvariant(),
                PlanName = activeSubscription?.PackageNameSnapshot ?? package?.Name ?? _freePlan.Business.Name,
                Description = package?.Description ?? _freePlan.Business.Description,
                Role = user.Role,
                SubscriptionId = activeSubscription?.SubscriptionId,
                ActivatedAt = activeSubscription?.ActivatedAt,
                ExpiresAt = activeSubscription?.ExpiresAt,
                Ai = new PlanAiUsageResponseDto
                {
                    Feature = "SUPPLIER_MATCH",
                    DailyLimit = quota.Limit,
                    UsedToday = Math.Max(0, quota.Limit - quota.Remaining),
                    RemainingToday = quota.Remaining,
                    ResetsAt = quota.ResetsAt
                },
                SupplierMatching = new SupplierMatchingBenefitsResponseDto
                {
                    ResultLimit = entitlement.ResultLimit,
                    AiRerankingEnabled = entitlement.AiRerankingEnabled,
                    AdvancedFiltersEnabled = entitlement.AdvancedFiltersEnabled,
                    DetailedReasonsEnabled = entitlement.DetailedReasonsEnabled,
                    NewSupplierNotificationsEnabled = entitlement.Tier == SupplierMatchTier.Vip ||
                                                      _freePlan.Business.NewSupplierNotificationsEnabled
                }
            });
        }

        // Chỉ dùng để giải thích lý do; quyết định đủ điều kiện luôn lấy từ GetPriorityOwnerIdsAsync.
        private async Task<string> GetPostingPriorityBlockedReasonAsync(
            user user,
            bool hasActiveSubscription,
            CancellationToken cancellationToken)
        {
            if (!hasActiveSubscription)
                return "SUBSCRIPTION_INACTIVE";
            if (user.Status != UserStatus.Active)
                return "ACCOUNT_INACTIVE";
            if (user.Role == UserRole.Personal)
                return "PROFILE_REQUIRED";

            var profile = await _businessProfileRepository.GetByUserIdAsync(user.UserId, cancellationToken);
            return profile?.Status switch
            {
                null => "PROFILE_REQUIRED",
                (int)BusinessProfileStatus.Pending => "BUSINESS_PROFILE_PENDING",
                (int)BusinessProfileStatus.Rejected => "BUSINESS_PROFILE_REJECTED",
                _ => "SUBSCRIPTION_INACTIVE"
            };
        }

        public async Task<int> WarnExpiringSubscriptionsAsync(
            int batchSize,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var normalizedNow = nowUtc.Kind == DateTimeKind.Utc
                ? nowUtc
                : nowUtc.ToUniversalTime();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriptions = await _repository.GetExpiringWithoutWarningAsync(
                    normalizedNow,
                    normalizedNow.AddHours(24),
                    ExpirationWarningTitle,
                    batchSize,
                    cancellationToken);

                foreach (var subscription in subscriptions)
                {
                    var notification = await _notifications.AddPendingAsync(
                        new CreateNotificationCommand(
                            subscription.UserId,
                            ExpirationWarningTitle,
                            ExpirationWarningMessage,
                            NotificationTargetType.Subscription,
                            subscription.SubscriptionId),
                        cancellationToken);

                    _unitOfWork.RegisterAfterCommit(
                        () => _notifications.PublishCreatedSafelyAsync(notification));
                }

                if (subscriptions.Count > 0)
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return subscriptions.Count;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _unitOfWork.ClearTrackedEntities();
                throw;
            }
        }

        public Result<PlanDefinitionResponseDto> GetFreePlan(UserRole role)
        {
            if (role == UserRole.Personal)
            {
                return Result<PlanDefinitionResponseDto>.Success(new PlanDefinitionResponseDto
                {
                    PlanName = _freePlan.Personal.Name,
                    Description = _freePlan.Personal.Description,
                    Role = role,
                    AiFeature = "PRICE_SUGGESTION",
                    AiDailyLimit = Math.Max(0, _freePlan.Personal.PriceSuggestionDailyLimit)
                });
            }

            if (role == UserRole.Business)
            {
                return Result<PlanDefinitionResponseDto>.Success(new PlanDefinitionResponseDto
                {
                    PlanName = _freePlan.Business.Name,
                    Description = _freePlan.Business.Description,
                    Role = role,
                    AiFeature = "SUPPLIER_MATCH",
                    AiDailyLimit = Math.Max(0, _freePlan.Business.SupplierMatchDailyLimit),
                    SupplierMatching = new SupplierMatchingBenefitsResponseDto
                    {
                        ResultLimit = Math.Clamp(_freePlan.Business.SupplierMatchResultLimit, 1, 100),
                        AiRerankingEnabled = _freePlan.Business.AiRerankingEnabled,
                        AdvancedFiltersEnabled = _freePlan.Business.AdvancedFiltersEnabled,
                        DetailedReasonsEnabled = _freePlan.Business.DetailedReasonsEnabled,
                        NewSupplierNotificationsEnabled = _freePlan.Business.NewSupplierNotificationsEnabled
                    }
                });
            }

            return Result<PlanDefinitionResponseDto>.Fail(
                ValidationErrors.InvalidRequest("TargetRole chỉ hỗ trợ Personal hoặc Business."));
        }

        private static List<user_subscription_entitlement> SnapshotEntitlements(
            Guid subscriptionId,
            subscription_package package,
            DateTime createdAtUtc)
        {
            return package.Entitlements
                .Select(x => new user_subscription_entitlement
                {
                    SubscriptionEntitlementId = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    EntitlementKey = x.EntitlementKey,
                    ValueType = x.ValueType,
                    NumericValue = x.NumericValue,
                    BooleanValue = x.BooleanValue,
                    IsUnlimited = x.IsUnlimited,
                    CreatedAt = createdAtUtc
                })
                .ToList();
        }
    }
}
