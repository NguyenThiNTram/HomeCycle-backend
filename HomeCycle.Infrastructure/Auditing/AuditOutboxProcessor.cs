using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Auditing
{
    public sealed class AuditOutboxProcessor : IAuditOutboxProcessor
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly HomeCycleDbContext _db;
        private readonly AuditLogOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AuditOutboxProcessor> _logger;

        public AuditOutboxProcessor(
            HomeCycleDbContext db,
            IOptions<AuditLogOptions> options,
            TimeProvider timeProvider,
            ILogger<AuditOutboxProcessor> logger)
        {
            _db = db;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<int> ProcessBatchAsync(
            CancellationToken cancellationToken = default)
        {
            var claims = await ClaimBatchAsync(
                cancellationToken);

            var processed = 0;

            foreach (var claim in claims)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await ProcessOneAsync(
                        claim,
                        cancellationToken))
                    {
                        processed++;
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (JsonException exception)
                {
                    _logger.LogError(
                        exception,
                        "Audit outbox {EventId} chứa payload không hợp lệ.",
                        claim.EventId);

                    await MarkPermanentFailureAsync(
                        claim,
                        "INVALID_AUDIT_PAYLOAD",
                        cancellationToken);
                }
                catch (InvalidDataException exception)
                {
                    _logger.LogError(
                        exception,
                        "Audit outbox {EventId} không hợp lệ.",
                        claim.EventId);

                    await MarkPermanentFailureAsync(
                        claim,
                        "INVALID_AUDIT_PAYLOAD",
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Không thể xử lý audit outbox {EventId}.",
                        claim.EventId);

                    await ScheduleRetryAsync(
                        claim,
                        exception.GetType().Name,
                        cancellationToken);
                }
            }

            return processed;
        }

        private async Task<IReadOnlyList<AuditOutboxClaim>>
            ClaimBatchAsync(
                CancellationToken cancellationToken)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var leaseId = Guid.NewGuid();

            var leaseUntil = now.AddSeconds(
                _options.Worker.ProcessingLeaseSeconds);

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var candidates = await _db.Audit_Outboxes
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Audit_Outbox"
                    WHERE "FailedAtUtc" IS NULL
                      AND "NextAttemptAtUtc" <= {now}
                      AND (
                          "LeaseUntilUtc" IS NULL
                          OR "LeaseUntilUtc" <= {now}
                      )
                    ORDER BY "CreatedAtUtc"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Worker.BatchSize}
                    """)
                .ToListAsync(cancellationToken);

            foreach (var candidate in candidates)
            {
                candidate.LeaseId = leaseId;
                candidate.LeaseUntilUtc = leaseUntil;
            }

            if (candidates.Count > 0)
            {
                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);

            var claims = candidates
                .Select(x => new AuditOutboxClaim(
                    x.EventId,
                    leaseId,
                    x.Payload,
                    x.RetryCount))
                .ToArray();

            _db.ChangeTracker.Clear();

            return claims;
        }

        private async Task<bool> ProcessOneAsync(
            AuditOutboxClaim claim,
            CancellationToken cancellationToken)
        {
            var payload =
                JsonSerializer.Deserialize<AuditOutboxPayload>(
                    claim.Payload,
                    JsonOptions)
                ?? throw new InvalidDataException(
                    "Audit payload rỗng.");

            if (payload.EventId != claim.EventId)
            {
                throw new InvalidDataException(
                    "Audit payload EventId không khớp Outbox EventId.");
            }

            if (string.IsNullOrWhiteSpace(payload.Action))
            {
                throw new InvalidDataException(
                    "Audit payload thiếu Action.");
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var outbox = await _db.Audit_Outboxes
                    .FirstOrDefaultAsync(
                        x =>
                            x.EventId == claim.EventId &&
                            x.LeaseId == claim.LeaseId &&
                            x.FailedAtUtc == null,
                        cancellationToken);

                if (outbox is null)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return false;
                }

                var alreadyPersisted =
                    await _db.Audit_Logs
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.EventId == claim.EventId,
                            cancellationToken);

                if (!alreadyPersisted)
                {
                    var now =
                        _timeProvider.GetUtcNow().UtcDateTime;

                    var auditLog = new audit_log
                    {
                        AuditId = Guid.NewGuid(),
                        EventId = payload.EventId,

                        Category = payload.Category,
                        Action = payload.Action,
                        Outcome = payload.Outcome,
                        ReasonCode = payload.ReasonCode,

                        ActorType = payload.ActorType,
                        UserId = payload.UserId,
                        UserRole = payload.UserRole,

                        TargetType = payload.TargetType,
                        TargetId = payload.TargetId,

                        OldValues =
                            payload.OldValues?.ToJsonString(),

                        NewValues =
                            payload.NewValues?.ToJsonString(),

                        Source = payload.Source,
                        CorrelationId = payload.CorrelationId,
                        IpAddress = payload.IpAddress,
                        UserAgent = payload.UserAgent,

                        OccurredAtUtc = payload.OccurredAtUtc,
                        RecordedAtUtc = now,

                        Metadata =
                            payload.Metadata?.ToJsonString()
                    };

                    await _db.Audit_Logs.AddAsync(
                        auditLog.ToInfrastructure(),
                        cancellationToken);
                }

                _db.Audit_Outboxes.Remove(outbox);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                _db.ChangeTracker.Clear();

                throw;
            }
        }

        private async Task ScheduleRetryAsync(
            AuditOutboxClaim claim,
            string errorCode,
            CancellationToken cancellationToken)
        {
            _db.ChangeTracker.Clear();

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var retryCount = claim.RetryCount + 1;

            if (retryCount >= _options.Worker.RetryLimit)
            {
                await MarkPermanentFailureAsync(
                    claim,
                    errorCode,
                    cancellationToken);

                return;
            }

            var multiplier = Math.Pow(
                2,
                Math.Max(0, retryCount - 1));

            var delaySeconds = Math.Min(
                _options.Worker.RetryBaseDelaySeconds *
                multiplier,
                3600);

            var nextAttempt =
                now.AddSeconds(delaySeconds);

            await _db.Audit_Outboxes
                .Where(x =>
                    x.EventId == claim.EventId &&
                    x.LeaseId == claim.LeaseId &&
                    x.FailedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.RetryCount,
                            retryCount)
                        .SetProperty(
                            x => x.NextAttemptAtUtc,
                            nextAttempt)
                        .SetProperty(
                            x => x.LeaseId,
                            (Guid?)null)
                        .SetProperty(
                            x => x.LeaseUntilUtc,
                            (DateTime?)null)
                        .SetProperty(
                            x => x.LastError,
                            Limit(errorCode, 500)),
                    cancellationToken);
        }

        private async Task MarkPermanentFailureAsync(
            AuditOutboxClaim claim,
            string errorCode,
            CancellationToken cancellationToken)
        {
            _db.ChangeTracker.Clear();

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            await _db.Audit_Outboxes
                .Where(x =>
                    x.EventId == claim.EventId &&
                    x.LeaseId == claim.LeaseId &&
                    x.FailedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.RetryCount,
                            x => x.RetryCount + 1)
                        .SetProperty(
                            x => x.FailedAtUtc,
                            now)
                        .SetProperty(
                            x => x.LeaseId,
                            (Guid?)null)
                        .SetProperty(
                            x => x.LeaseUntilUtc,
                            (DateTime?)null)
                        .SetProperty(
                            x => x.LastError,
                            Limit(errorCode, 500)),
                    cancellationToken);
        }

        private static string Limit(
            string value,
            int maxLength)
        {
            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }

        private sealed record AuditOutboxClaim(
            Guid EventId,
            Guid LeaseId,
            string Payload,
            int RetryCount);
    }
}
