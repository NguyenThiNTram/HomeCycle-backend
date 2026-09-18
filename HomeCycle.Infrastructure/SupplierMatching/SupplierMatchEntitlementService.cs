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
    IEntitlementResolver resolver) : ISupplierMatchEntitlementService
{
    public async Task<SupplierMatchEntitlement> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var limit = await resolver.ResolveAiDailyLimitAsync(userId, UserRole.Business, clock.GetUtcNow().UtcDateTime, cancellationToken);
        var isVip = limit == 100;
        var settings = options.Value;
        return new SupplierMatchEntitlement(
            isVip ? SupplierMatchTier.Vip : SupplierMatchTier.Free,
            Math.Clamp(isVip ? settings.VipResultLimit : settings.FreeResultLimit, 1, 100),
            limit,
            limit > 0,
            isVip,
            isVip);
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveVipUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.User_Subscriptions.AsNoTracking()
            .Where(x => x.Status == (int)UserSubscriptionStatus.Active &&
                x.ActivatedAt.HasValue && x.ActivatedAt <= now && x.ExpiresAt.HasValue && x.ExpiresAt > now &&
                x.User.Role == (int)UserRole.Business &&
                x.User_Subscription_Entitlements.Any(e => e.EntitlementKey == EntitlementKeys.SupplierMatchDailyCount &&
                    e.ValueType == (int)EntitlementValueType.Integer && e.NumericValue == 100 && !e.IsUnlimited && e.BooleanValue == null))
            .Select(x => x.UserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
