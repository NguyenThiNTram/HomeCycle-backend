using HomeCycle.Application.Entitlements;
using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Entitlements;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Entitlements
{
    public class EntitlementResolver : IEntitlementResolver
    {
        private readonly IUserSubscriptionRepository _repository;
        private readonly IUserRepository _users;
        private readonly FreePlanOptions _freePlan;

        public EntitlementResolver(
            IUserSubscriptionRepository repository,
            IUserRepository users,
            FreePlanOptions freePlan)
        {
            _repository = repository;
            _users = users;
            _freePlan = freePlan;
        }

        public async Task<EffectiveWithdrawalEntitlements> ResolveWithdrawalAsync(
            Guid userId,
            decimal baselineDailyAmountLimit,
            int baselineDailyCountLimit,
            DateTime atUtc,
            CancellationToken cancellationToken = default)
        {
            var user = await _users.GetByIdAsync(userId, cancellationToken);
            if (user?.Role != UserRole.Business)
                return new EffectiveWithdrawalEntitlements(baselineDailyAmountLimit, baselineDailyCountLimit, null);

            var subscription = await _repository.GetActiveWithEntitlementsAsync(
                userId,
                atUtc,
                cancellationToken);

            if (subscription == null)
            {
                return new EffectiveWithdrawalEntitlements(
                    baselineDailyAmountLimit,
                    baselineDailyCountLimit,
                    null);
            }

            var amountEntitlement = subscription.Entitlements.SingleOrDefault(x =>
                string.Equals(
                    x.EntitlementKey,
                    EntitlementKeys.WithdrawalDailyAmount,
                    StringComparison.OrdinalIgnoreCase));

            var countEntitlement = subscription.Entitlements.SingleOrDefault(x =>
                string.Equals(
                    x.EntitlementKey,
                    EntitlementKeys.WithdrawalDailyCount,
                    StringComparison.OrdinalIgnoreCase));

            return new EffectiveWithdrawalEntitlements(
                ResolveDailyAmount(amountEntitlement, baselineDailyAmountLimit),
                ResolveDailyCount(countEntitlement, baselineDailyCountLimit),
                subscription.SubscriptionId);
        }

        public async Task<int> ResolveAiDailyLimitAsync(Guid userId, UserRole role, DateTime atUtc, CancellationToken cancellationToken = default)
        {
            if (role is not (UserRole.Personal or UserRole.Business))
                throw new ArgumentOutOfRangeException(nameof(role));
            var user = await _users.GetByIdAsync(userId, cancellationToken);
            if (user?.Role != role)
                return 0;
            var subscription = await _repository.GetActiveWithEntitlementsAsync(userId, atUtc, cancellationToken);
            var key = role == UserRole.Personal ? EntitlementKeys.PriceSuggestionDailyCount : EntitlementKeys.SupplierMatchDailyCount;
            var entitlement = subscription?.Entitlements.SingleOrDefault(x => x.EntitlementKey.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (entitlement == null)
                return role == UserRole.Personal
                    ? Math.Max(0, _freePlan.Personal.PriceSuggestionDailyLimit)
                    : Math.Max(0, _freePlan.Business.SupplierMatchDailyLimit);
            if (entitlement.ValueType != EntitlementValueType.Integer || entitlement.IsUnlimited || entitlement.BooleanValue.HasValue
                || !entitlement.NumericValue.HasValue || entitlement.NumericValue.Value <= 0 || entitlement.NumericValue.Value > int.MaxValue
                || decimal.Truncate(entitlement.NumericValue.Value) != entitlement.NumericValue.Value
                || (role == UserRole.Personal && entitlement.NumericValue.Value != 50))
                throw new InvalidOperationException($"Invalid snapshot value for entitlement '{key}'.");
            return decimal.ToInt32(entitlement.NumericValue.Value);
        }

        private static decimal? ResolveDailyAmount(
            user_subscription_entitlement? entitlement,
            decimal baseline)
        {
            if (entitlement == null)
                return baseline;

            if (entitlement.ValueType == EntitlementValueType.Decimal && entitlement.IsUnlimited &&
                !entitlement.NumericValue.HasValue && !entitlement.BooleanValue.HasValue)
                return null;

            if (entitlement.ValueType != EntitlementValueType.Decimal ||
                entitlement.IsUnlimited ||
                !entitlement.NumericValue.HasValue ||
                entitlement.NumericValue.Value <= 0 ||
                entitlement.BooleanValue.HasValue)
            {
                throw new InvalidOperationException(
                    $"Invalid snapshot value for entitlement '{EntitlementKeys.WithdrawalDailyAmount}'.");
            }

            return entitlement.NumericValue.Value;
        }

        private static int? ResolveDailyCount(
            user_subscription_entitlement? entitlement,
            int baseline)
        {
            if (entitlement == null)
                return baseline;

            if (entitlement.ValueType != EntitlementValueType.Integer ||
                entitlement.BooleanValue.HasValue)
            {
                throw new InvalidOperationException(
                    $"Invalid snapshot value for entitlement '{EntitlementKeys.WithdrawalDailyCount}'.");
            }

            if (entitlement.IsUnlimited)
            {
                if (entitlement.NumericValue.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Invalid snapshot value for entitlement '{EntitlementKeys.WithdrawalDailyCount}'.");
                }

                return null;
            }

            if (!entitlement.NumericValue.HasValue ||
                entitlement.NumericValue.Value <= 0 ||
                decimal.Truncate(entitlement.NumericValue.Value) != entitlement.NumericValue.Value ||
                entitlement.NumericValue.Value > int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Invalid snapshot value for entitlement '{EntitlementKeys.WithdrawalDailyCount}'.");
            }

            return decimal.ToInt32(entitlement.NumericValue.Value);
        }
    }
}
