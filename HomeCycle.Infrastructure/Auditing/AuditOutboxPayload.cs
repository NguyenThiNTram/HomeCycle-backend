using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Auditing
{
    internal sealed class AuditOutboxPayload
    {
        public Guid EventId { get; set; }

        public AuditCategory Category { get; set; }
        public string Action { get; set; } = null!;
        public AuditOutcome Outcome { get; set; }
        public string? ReasonCode { get; set; }

        public AuditActorType ActorType { get; set; }
        public Guid? UserId { get; set; }
        public UserRole? UserRole { get; set; }

        public string? TargetType { get; set; }
        public Guid? TargetId { get; set; }

        public JsonNode? OldValues { get; set; }
        public JsonNode? NewValues { get; set; }

        public AuditSource Source { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public JsonNode? Metadata { get; set; }
    }
}
