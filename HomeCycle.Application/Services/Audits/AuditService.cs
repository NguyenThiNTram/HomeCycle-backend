using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Repositories.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Audits
{
    public sealed class AuditService : IAuditService
    {
        private readonly IAuditOutboxWriter _outboxWriter;
        private readonly IAuditContextAccessor _contextAccessor;
        private readonly TimeProvider _timeProvider;

        public AuditService(
            IAuditOutboxWriter outboxWriter,
            IAuditContextAccessor contextAccessor,
            TimeProvider timeProvider)
        {
            _outboxWriter = outboxWriter;
            _contextAccessor = contextAccessor;
            _timeProvider = timeProvider;
        }

        public async Task EnqueueAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);

            Validate(auditEvent);

            var context = _contextAccessor.Capture();
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var actorType = auditEvent.ActorType ??
                (context.IsHttpRequest
                    ? context.UserId.HasValue
                        ? AuditActorType.User
                        : AuditActorType.Anonymous
                    : AuditActorType.System);

            var source = auditEvent.Source ??
                (context.IsHttpRequest
                    ? AuditSource.HttpApi
                    : AuditSource.Internal);

            var useHttpUserContext = actorType == AuditActorType.User;

            var record = new AuditRecord
            {
                EventId = auditEvent.EventId,
                Category = auditEvent.Category,
                Action = auditEvent.Action.Trim(),
                Outcome = auditEvent.Outcome,
                ReasonCode = Normalize(auditEvent.ReasonCode),

                ActorType = actorType,
                UserId = auditEvent.UserId ??
                         (useHttpUserContext ? context.UserId : null),
                UserRole = auditEvent.UserRole ??
                           (useHttpUserContext ? context.UserRole : null),

                TargetType = Normalize(auditEvent.TargetType),
                TargetId = auditEvent.TargetId,

                OldValues = auditEvent.OldValues,
                NewValues = auditEvent.NewValues,

                Source = source,
                CorrelationId =
                    Normalize(auditEvent.CorrelationId) ??
                    context.CorrelationId,

                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,

                OccurredAtUtc = NormalizeUtc(
                    auditEvent.OccurredAtUtc ?? now),

                Metadata = auditEvent.Metadata
            };

            await _outboxWriter.EnqueueAsync(
                record,
                cancellationToken);
        }

        private static void Validate(AuditEvent auditEvent)
        {
            if (auditEvent.EventId == Guid.Empty)
                throw new ArgumentException(
                    "Audit EventId không được rỗng.",
                    nameof(auditEvent));

            if (!Enum.IsDefined(typeof(AuditCategory), auditEvent.Category))
                throw new ArgumentException(
                    "Audit Category không hợp lệ.",
                    nameof(auditEvent));

            if (!Enum.IsDefined(typeof(AuditOutcome), auditEvent.Outcome))
                throw new ArgumentException(
                    "Audit Outcome không hợp lệ.",
                    nameof(auditEvent));

            if (string.IsNullOrWhiteSpace(auditEvent.Action))
                throw new ArgumentException(
                    "Audit Action là bắt buộc.",
                    nameof(auditEvent));

            if (auditEvent.Action.Trim().Length > 100)
                throw new ArgumentException(
                    "Audit Action không được vượt quá 100 ký tự.",
                    nameof(auditEvent));

            if (auditEvent.ReasonCode?.Trim().Length > 150)
                throw new ArgumentException(
                    "Audit ReasonCode không được vượt quá 150 ký tự.",
                    nameof(auditEvent));

            if (auditEvent.TargetType?.Trim().Length > 100)
                throw new ArgumentException(
                    "Audit TargetType không được vượt quá 100 ký tự.",
                    nameof(auditEvent));

            if (auditEvent.CorrelationId?.Trim().Length > 100)
                throw new ArgumentException(
                    "Audit CorrelationId không được vượt quá 100 ký tự.",
                    nameof(auditEvent));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
