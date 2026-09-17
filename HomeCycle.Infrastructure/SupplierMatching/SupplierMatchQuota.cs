using System.Data;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class SupplierMatchQuota(HomeCycleDbContext db, TimeProvider clock) : ISupplierMatchQuota
{
    private static readonly TimeZoneInfo Vietnam = ResolveVietnamTimeZone();

    public async Task<SupplierMatchQuotaStatus> GetRemainingAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Max(1, limit);
        var today = GetToday();
        var count = await db.SupplierMatchDailyUsages.AsNoTracking()
            .Where(usage => usage.UserId == userId && usage.UsageDate == today)
            .Select(usage => (int?)usage.RefreshCount)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;
        return new SupplierMatchQuotaStatus(limit, Math.Max(0, limit - count), GetResetTime(today));
    }

    public async Task<SupplierMatchQuotaReservation> TryReserveAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Max(1, limit);
        var today = GetToday();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO public."SupplierMatchDailyUsage"
                    ("UserId", "UsageDate", "RefreshCount", "CreatedAt", "UpdatedAt")
                VALUES (@userId, @day, 1, now(), now())
                ON CONFLICT ("UserId", "UsageDate")
                DO UPDATE SET
                    "RefreshCount" = public."SupplierMatchDailyUsage"."RefreshCount" + 1,
                    "UpdatedAt" = now()
                WHERE public."SupplierMatchDailyUsage"."RefreshCount" < @limit
                RETURNING "RefreshCount"
                """, connection);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("day", today);
            command.Parameters.AddWithValue("limit", limit);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
                return new SupplierMatchQuotaReservation(false, limit, 0, GetResetTime(today));
            var count = Convert.ToInt32(value);
            return new SupplierMatchQuotaReservation(true, limit, Math.Max(0, limit - count), GetResetTime(today));
        }
        finally
        {
            if (shouldClose) await db.Database.CloseConnectionAsync();
        }
    }

    private DateOnly GetToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Vietnam).Date);

    private static DateTimeOffset GetResetTime(DateOnly today)
    {
        var localMidnight = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localMidnight, Vietnam.GetUtcOffset(localMidnight));
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }
}
