using System.Text.RegularExpressions;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed partial class SupplierMatchEntitlementService(
    HomeCycleDbContext db,
    IOptions<SupplierMatchingOptions> options,
    TimeProvider clock) : ISupplierMatchEntitlementService
{
    public async Task<SupplierMatchEntitlement> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var activeStatuses = settings.ActiveSubscriptionStatuses ?? [];
        var now = clock.GetUtcNow().UtcDateTime;
        var packageNames = await db.User_Subscriptions.AsNoTracking()
            .Where(subscription =>
                subscription.UserId == userId &&
                subscription.Package.IsActive &&
                (!subscription.ActivatedAt.HasValue || subscription.ActivatedAt <= now) &&
                (!subscription.ExpiresAt.HasValue || subscription.ExpiresAt > now) &&
                (!subscription.Status.HasValue || activeStatuses.Contains(subscription.Status.Value)))
            .OrderByDescending(subscription => subscription.ExpiresAt)
            .Select(subscription => subscription.Package.Name)
            .ToListAsync(cancellationToken);

        var isVip = packageNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => NormalizeCode(name!))
            .Any(name => IsVipPackageName(name, settings));

        return isVip
            ? new SupplierMatchEntitlement(
                SupplierMatchTier.Vip,
                Math.Clamp(settings.VipResultLimit, 1, 100),
                Math.Clamp(settings.VipDailyAiRefreshLimit, 1, 1000),
                true,
                true,
                true)
            : new SupplierMatchEntitlement(
                SupplierMatchTier.Free,
                Math.Clamp(settings.FreeResultLimit, 1, 100),
                Math.Clamp(settings.FreeDailyAiRefreshLimit, 1, 1000),
                true,
                false,
                false);
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveVipUserIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var activeStatuses = settings.ActiveSubscriptionStatuses ?? [];
        var now = clock.GetUtcNow().UtcDateTime;
        var subscriptions = await db.User_Subscriptions.AsNoTracking()
            .Where(subscription =>
                subscription.Package.IsActive &&
                (!subscription.ActivatedAt.HasValue || subscription.ActivatedAt <= now) &&
                (!subscription.ExpiresAt.HasValue || subscription.ExpiresAt > now) &&
                (!subscription.Status.HasValue || activeStatuses.Contains(subscription.Status.Value)))
            .Select(subscription => new
            {
                subscription.UserId,
                subscription.Package.Name
            })
            .ToListAsync(cancellationToken);

        return subscriptions
            .Where(subscription => !string.IsNullOrWhiteSpace(subscription.Name) &&
                IsVipPackageName(NormalizeCode(subscription.Name!), settings))
            .Select(subscription => subscription.UserId)
            .Distinct()
            .ToArray();
    }

    private static bool IsVipPackageName(string normalizedName, SupplierMatchingOptions settings) =>
        (settings.VipPackageCodes ?? [])
        .Select(NormalizeCode)
        .Where(code => code.Length > 0)
        .Any(code => normalizedName.Contains(code, StringComparison.Ordinal));

    private static string NormalizeCode(string value) =>
        NonAlphaNumeric().Replace(value.Trim().ToUpperInvariant(), string.Empty);

    [GeneratedRegex("[^A-Z0-9]")]
    private static partial Regex NonAlphaNumeric();
}
