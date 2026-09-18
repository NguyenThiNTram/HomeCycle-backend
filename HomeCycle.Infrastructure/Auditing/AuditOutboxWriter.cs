using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Repositories.Audits;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Auditing
{
    public sealed class AuditOutboxWriter : IAuditOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly HomeCycleDbContext _db;
        private readonly AuditPayloadSanitizer _sanitizer;
        private readonly AuditLogOptions _options;
        private readonly TimeProvider _timeProvider;

        public AuditOutboxWriter(
            HomeCycleDbContext db,
            AuditPayloadSanitizer sanitizer,
            IOptions<AuditLogOptions> options,
            TimeProvider timeProvider)
        {
            _db = db;
            _sanitizer = sanitizer;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task EnqueueAsync(
            AuditRecord auditRecord,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditRecord);

            var payload = new AuditOutboxPayload
            {
                EventId = auditRecord.EventId,
                Category = auditRecord.Category,
                Action = auditRecord.Action,
                Outcome = auditRecord.Outcome,
                ReasonCode = auditRecord.ReasonCode,

                ActorType = auditRecord.ActorType,
                UserId = auditRecord.UserId,
                UserRole = auditRecord.UserRole,

                TargetType = auditRecord.TargetType,
                TargetId = auditRecord.TargetId,

                OldValues =
                    _sanitizer.Sanitize(auditRecord.OldValues),

                NewValues =
                    _sanitizer.Sanitize(auditRecord.NewValues),

                Source = auditRecord.Source,
                CorrelationId = auditRecord.CorrelationId,
                IpAddress = auditRecord.IpAddress,
                UserAgent = auditRecord.UserAgent,

                OccurredAtUtc = auditRecord.OccurredAtUtc,

                Metadata =
                    _sanitizer.Sanitize(auditRecord.Metadata)
            };

            var serializedPayload =
                JsonSerializer.Serialize(payload, JsonOptions);

            var payloadSize = Encoding.UTF8.GetByteCount(
                serializedPayload);

            if (payloadSize > _options.Payload.MaxBytes)
            {
                throw new InvalidOperationException(
                    $"Audit payload vượt quá giới hạn {_options.Payload.MaxBytes} bytes.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var outbox = new Audit_Outbox
            {
                EventId = auditRecord.EventId,
                Payload = serializedPayload,

                RetryCount = 0,
                NextAttemptAtUtc = now,

                LeaseId = null,
                LeaseUntilUtc = null,

                FailedAtUtc = null,
                LastError = null,

                CreatedAtUtc = now
            };

            await _db.Audit_Outboxes.AddAsync(
                outbox,
                cancellationToken);
        }
    }
}
