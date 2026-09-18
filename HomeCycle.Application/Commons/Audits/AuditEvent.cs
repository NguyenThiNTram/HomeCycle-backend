using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Audits
{
    public sealed record AuditEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public AuditCategory Category { get; init; }
        public string Action { get; init; } = null!;
        public AuditOutcome Outcome { get; init; }
        public string? ReasonCode { get; init; }

        public AuditActorType? ActorType { get; init; }
        public Guid? UserId { get; init; }
        public UserRole? UserRole { get; init; }

        public string? TargetType { get; init; }
        public Guid? TargetId { get; init; }

        public IReadOnlyDictionary<string, object?>? OldValues { get; init; }
        public IReadOnlyDictionary<string, object?>? NewValues { get; init; }

        public AuditSource? Source { get; init; }
        public string? CorrelationId { get; init; }

        public DateTime? OccurredAtUtc { get; init; }

        public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
    }
}
