using System.Data;
using HomeCycle.Application.Interfaces.Repositories.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeCycle.Infrastructure.Repositories.SupplierMatching;

public sealed class SupplierMatchMonitorRepository(HomeCycleDbContext db) : ISupplierMatchMonitorRepository
{
    public Task DisableClosedOrExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.SupplierMatchMonitorStates
            .Where(state => state.IsActive && !db.Posts.Any(post =>
                post.PostId == state.BuyPostId &&
                post.PostType == (int)PostType.Buy &&
                post.Status == (int)PostStatus.Active &&
                post.RemainingQuantity > 0 &&
                (!post.ExpiryDate.HasValue || post.ExpiryDate > now.UtcDateTime)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(state => state.IsActive, false)
                .SetProperty(state => state.LastCheckedAt, now.UtcDateTime)
                .SetProperty(state => state.UpdatedAt, now.UtcDateTime), cancellationToken);

    public async Task<IReadOnlyList<SupplierMatchMonitorTarget>> GetTargetsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        batchSize = Math.Clamp(batchSize, 1, 200);
        var now = DateTime.UtcNow;
        return await (
                from post in db.Posts.AsNoTracking()
                join state in db.SupplierMatchMonitorStates.AsNoTracking()
                    on post.PostId equals state.BuyPostId into states
                from state in states.DefaultIfEmpty()
                where post.PostType == (int)PostType.Buy &&
                      post.Status == (int)PostStatus.Active &&
                      post.RemainingQuantity > 0 &&
                      (!post.ExpiryDate.HasValue || post.ExpiryDate > now) &&
                      post.User != null && post.User.Status == (int)UserStatus.Active
                orderby state == null ? DateTime.MinValue : state.LastCheckedAt, post.PostId
                select new SupplierMatchMonitorTarget(post.PostId, post.OwnerId))
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierMatchMonitorStateSnapshot?> GetStateAsync(
        Guid buyPostId,
        CancellationToken cancellationToken = default) =>
        await db.SupplierMatchMonitorStates.AsNoTracking()
            .Where(state => state.BuyPostId == buyPostId)
            .Select(state => new SupplierMatchMonitorStateSnapshot(
                state.BuyPostId, state.UserId, state.LastBestScore,
                new DateTimeOffset(state.LastCheckedAt, TimeSpan.Zero), state.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task InitializeAsync(
        Guid buyPostId,
        Guid userId,
        decimal bestScore,
        IReadOnlyList<SupplierMatchMonitorCandidate> candidates,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await UpdateStateAsync(buyPostId, userId, bestScore, now, cancellationToken);
        foreach (var candidate in candidates)
        {
            await ExecuteAsync("""
                INSERT INTO public."SupplierMatchNotificationDedupe"
                    ("BuyPostId", "SellPostId", "LastScore", "FirstSeenAt", "LastSeenAt", "LastNotifiedAt")
                VALUES (@buyPostId, @sellPostId, @score, @now, @now, @now)
                ON CONFLICT ("BuyPostId", "SellPostId") DO UPDATE SET
                    "LastScore" = EXCLUDED."LastScore",
                    "LastSeenAt" = EXCLUDED."LastSeenAt"
                """, command =>
            {
                command.Parameters.AddWithValue("buyPostId", buyPostId);
                command.Parameters.AddWithValue("sellPostId", candidate.SellPostId);
                command.Parameters.AddWithValue("score", candidate.Score);
                command.Parameters.AddWithValue("now", now.UtcDateTime);
            }, cancellationToken);
        }
    }

    public async Task<bool> TryClaimImprovementAsync(
        Guid buyPostId,
        Guid sellPostId,
        decimal score,
        decimal minimumDelta,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var value = await ExecuteScalarAsync("""
            INSERT INTO public."SupplierMatchNotificationDedupe"
                ("BuyPostId", "SellPostId", "LastScore", "FirstSeenAt", "LastSeenAt", "LastNotifiedAt")
            VALUES (@buyPostId, @sellPostId, @score, @now, @now, @now)
            ON CONFLICT ("BuyPostId", "SellPostId") DO UPDATE SET
                "LastScore" = EXCLUDED."LastScore",
                "LastSeenAt" = EXCLUDED."LastSeenAt",
                "LastNotifiedAt" = EXCLUDED."LastNotifiedAt"
            WHERE EXCLUDED."LastScore" >= public."SupplierMatchNotificationDedupe"."LastScore" + @minimumDelta
            RETURNING 1
            """, command =>
        {
            command.Parameters.AddWithValue("buyPostId", buyPostId);
            command.Parameters.AddWithValue("sellPostId", sellPostId);
            command.Parameters.AddWithValue("score", score);
            command.Parameters.AddWithValue("minimumDelta", minimumDelta);
            command.Parameters.AddWithValue("now", now.UtcDateTime);
        }, cancellationToken);
        return value is not null and not DBNull;
    }

    public Task UpdateStateAsync(
        Guid buyPostId,
        Guid userId,
        decimal bestScore,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO public."SupplierMatchMonitorState"
                ("BuyPostId", "UserId", "LastBestScore", "LastCheckedAt", "IsActive", "CreatedAt", "UpdatedAt")
            VALUES (@buyPostId, @userId, @bestScore, @now, true, @now, @now)
            ON CONFLICT ("BuyPostId") DO UPDATE SET
                "UserId" = EXCLUDED."UserId",
                "LastBestScore" = EXCLUDED."LastBestScore",
                "LastCheckedAt" = EXCLUDED."LastCheckedAt",
                "IsActive" = true,
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, command =>
        {
            command.Parameters.AddWithValue("buyPostId", buyPostId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("bestScore", bestScore);
            command.Parameters.AddWithValue("now", now.UtcDateTime);
        }, cancellationToken);

    public Task DisableAsync(Guid buyPostId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.SupplierMatchMonitorStates.Where(state => state.BuyPostId == buyPostId && state.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(state => state.IsActive, false)
                .SetProperty(state => state.LastCheckedAt, now.UtcDateTime)
                .SetProperty(state => state.UpdatedAt, now.UtcDateTime), cancellationToken);

    private async Task ExecuteAsync(
        string sql,
        Action<NpgsqlCommand> configure,
        CancellationToken cancellationToken)
    {
        await ExecuteScalarAsync(sql, configure, cancellationToken, scalar: false);
    }

    private Task<object?> ExecuteScalarAsync(
        string sql,
        Action<NpgsqlCommand> configure,
        CancellationToken cancellationToken) =>
        ExecuteScalarAsync(sql, configure, cancellationToken, scalar: true);

    private async Task<object?> ExecuteScalarAsync(
        string sql,
        Action<NpgsqlCommand> configure,
        CancellationToken cancellationToken,
        bool scalar)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            configure(command);
            return scalar
                ? await command.ExecuteScalarAsync(cancellationToken)
                : await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose) await db.Database.CloseConnectionAsync();
        }
    }
}
