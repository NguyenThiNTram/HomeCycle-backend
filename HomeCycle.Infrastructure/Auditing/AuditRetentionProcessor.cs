using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Auditing
{
    public sealed class AuditRetentionProcessor : IAuditRetentionProcessor
    {
        private readonly HomeCycleDbContext _db;
        private readonly AuditLogOptions _options;
        private readonly TimeProvider _timeProvider;

        public AuditRetentionProcessor(HomeCycleDbContext db, IOptions<AuditLogOptions> options, TimeProvider timeProvider)
        {
            _db = db;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<(int AuditLogsDeleted, int FailedOutboxesDeleted)> CleanupAsync(CancellationToken cancellationToken = default)
        {
            var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.Retention.Days);

            var auditLogsDeleted = await DeleteExpiredAuditLogsAsync(cutoffUtc, cancellationToken);
            var failedOutboxesDeleted = await DeleteExpiredFailedOutboxesAsync(cutoffUtc, cancellationToken);

            return (auditLogsDeleted, failedOutboxesDeleted);
        }

        private async Task<int> DeleteExpiredAuditLogsAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
        {
            var totalDeleted = 0;
            var batchSize = _options.Retention.CleanupBatchSize;

            while (!cancellationToken.IsCancellationRequested)
            {
                var deleted = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    WITH expired AS (
                        SELECT "AuditId"
                        FROM "Audit_Log"
                        WHERE "OccurredAtUtc" < {cutoffUtc}
                        ORDER BY "OccurredAtUtc", "AuditId"
                        LIMIT {batchSize}
                        FOR UPDATE SKIP LOCKED
                    )
                    DELETE FROM "Audit_Log" AS audit
                    USING expired
                    WHERE audit."AuditId" = expired."AuditId";
                    """, cancellationToken);

                totalDeleted += deleted;

                if (deleted < batchSize)
                    break;
            }

            return totalDeleted;
        }

        private async Task<int> DeleteExpiredFailedOutboxesAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
        {
            var totalDeleted = 0;
            var batchSize = _options.Retention.CleanupBatchSize;

            while (!cancellationToken.IsCancellationRequested)
            {
                var deleted = await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    WITH expired AS (
                        SELECT "EventId"
                        FROM "Audit_Outbox"
                        WHERE "FailedAtUtc" IS NOT NULL
                          AND "FailedAtUtc" < {cutoffUtc}
                        ORDER BY "FailedAtUtc", "EventId"
                        LIMIT {batchSize}
                        FOR UPDATE SKIP LOCKED
                    )
                    DELETE FROM "Audit_Outbox" AS outbox
                    USING expired
                    WHERE outbox."EventId" = expired."EventId";
                    """, cancellationToken);

                totalDeleted += deleted;

                if (deleted < batchSize)
                    break;
            }

            return totalDeleted;
        }
    }
}
