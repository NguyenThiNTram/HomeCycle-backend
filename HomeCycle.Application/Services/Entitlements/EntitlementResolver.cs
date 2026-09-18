using HomeCycle.Application.Entitlements;
using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
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

        public EntitlementResolver(IUserSubscriptionRepository repository)
        {
            _repository = repository;
        }

        public async Task<EffectiveWithdrawalEntitlements> ResolveWithdrawalAsync(
            Guid userId,
            decimal baselineDailyAmountLimit,
            int baselineDailyCountLimit,
            DateTime atUtc,
            CancellationToken cancellationToken = default)
        {
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

        private static decimal ResolveDailyAmount(
            user_subscription_entitlement? entitlement,
            decimal baseline)
        {
            if (entitlement == null)
                return baseline;

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
