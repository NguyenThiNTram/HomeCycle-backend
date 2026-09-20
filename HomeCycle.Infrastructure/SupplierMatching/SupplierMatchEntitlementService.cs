using HomeCycle.Application.Entitlements;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.Entitlements;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class SupplierMatchEntitlementService(
    HomeCycleDbContext db,
    IOptions<SupplierMatchingOptions> options,
    TimeProvider clock,
    IEntitlementResolver resolver,
    FreePlanOptions freePlan) : ISupplierMatchEntitlementService
{
    public async Task<SupplierMatchEntitlement> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var limit = await resolver.ResolveAiDailyLimitAsync(userId, UserRole.Business, now, cancellationToken);
        var isVip = await ActiveAiSubscriptions(now).AnyAsync(x => x.UserId == userId, cancellationToken);
        var settings = options.Value;
        return new SupplierMatchEntitlement(
            isVip ? SupplierMatchTier.Vip : SupplierMatchTier.Free,
            Math.Clamp(isVip ? settings.VipResultLimit : freePlan.Business.SupplierMatchResultLimit, 1, 100),
            limit,
            isVip || freePlan.Business.AiRerankingEnabled,
            isVip || freePlan.Business.AdvancedFiltersEnabled,
            isVip || freePlan.Business.DetailedReasonsEnabled);
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveVipUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await ActiveAiSubscriptions(now).Select(x => x.UserId).Distinct().ToArrayAsync(cancellationToken);
    }

    private IQueryable<User_Subscription> ActiveAiSubscriptions(DateTime now)
        => db.User_Subscriptions.AsNoTracking()
            .Where(x => x.Status == (int)UserSubscriptionStatus.Active &&
                x.ActivatedAt.HasValue && x.ActivatedAt <= now && x.ExpiresAt.HasValue && x.ExpiresAt > now &&
                x.User.Role == (int)UserRole.Business &&
                x.User_Subscription_Entitlements.Any(e => e.EntitlementKey == EntitlementKeys.SupplierMatchDailyCount &&
                    e.ValueType == (int)EntitlementValueType.Integer && e.NumericValue.HasValue && e.NumericValue > 0 && e.NumericValue <= int.MaxValue
                    && e.NumericValue == Math.Truncate(e.NumericValue.Value) && !e.IsUnlimited && e.BooleanValue == null));
}
