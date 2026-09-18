using AutoMapper;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.SubscriptionPackages;
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
        private readonly IUserSubscriptionRepository _repository;
        private readonly ISubscriptionPackageRepository _packageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;

        public UserSubscriptionService(
            IUserSubscriptionRepository repository,
            ISubscriptionPackageRepository packageRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IMapper mapper)
        {
            _repository = repository;
            _packageRepository = packageRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _mapper = mapper;
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
            Guid packageId,
            DateTime createdAtUtc,
            CancellationToken cancellationToken = default)
        {
            var subscription = new user_subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PackageId = packageId,
                Status = (int)UserSubscriptionStatus.Pending,
                CreatedAt = createdAtUtc
            };

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

            var package = await _packageRepository.GetByIdForUpdateAsync(subscription.PackageId, cancellationToken);

            if (package == null)
                return Result<user_subscription>.Fail(SubscriptionPackageErrors.NotFound);

            subscription.PricePaid = pricePaid;
            subscription.Status = (int)UserSubscriptionStatus.Active;
            subscription.ActivatedAt = paidAtUtc;
            subscription.ExpiresAt = paidAtUtc.AddDays(package.Duration);
            subscription.Entitlements = SnapshotEntitlements(subscription.SubscriptionId, package, paidAtUtc);

            await _repository.UpdateAsync(subscription, cancellationToken);
            await _repository.ReplaceEntitlementsAsync(
                subscription.SubscriptionId,
                subscription.Entitlements,
                cancellationToken);

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

        public async Task<Result<UserSubscriptionResponseDto?>> GetCurrentAsync(
            Guid userId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _repository.GetOpenAsync(userId, nowUtc, cancellationToken);

            return Result<UserSubscriptionResponseDto?>.Success(
                subscription == null
                    ? null
                    : _mapper.Map<UserSubscriptionResponseDto>(subscription));
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
