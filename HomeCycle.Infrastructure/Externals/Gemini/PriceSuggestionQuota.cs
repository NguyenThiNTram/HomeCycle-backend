using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Application.Interfaces.Services.AI;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeCycle.Infrastructure.Externals.Gemini;

public sealed class PriceSuggestionQuota(HomeCycleDbContext db, TimeProvider clock) : IPriceSuggestionQuota
{
    public int DailyLimit => 5;
    private static readonly TimeZoneInfo Vietnam = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Vietnam).Date);

    public DateTimeOffset ResetsAt => new(Today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));

    public async Task<int> RemainingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var day = Today;
        var count = await db.PriceSuggestionDailyUsages.AsNoTracking()
            .Where(x => x.UserId == userId && x.UsageDate == day)
            .Select(x => (int?)x.UsageCount)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;
        return Math.Max(0, DailyLimit - count);
    }

    // PostgreSQL makes the reservation atomic across concurrent requests and app instances.
    public async Task<int?> ReserveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO public."PriceSuggestionDailyUsage" ("UserId", "UsageDate", "UsageCount")
                VALUES (@userId, @day, 1)
                ON CONFLICT ("UserId", "UsageDate")
                DO UPDATE SET "UsageCount" = "PriceSuggestionDailyUsage"."UsageCount" + 1
                WHERE "PriceSuggestionDailyUsage"."UsageCount" < 5
                RETURNING "UsageCount"
                """, connection);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("day", Today);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is null or DBNull ? null : DailyLimit - Convert.ToInt32(value);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
