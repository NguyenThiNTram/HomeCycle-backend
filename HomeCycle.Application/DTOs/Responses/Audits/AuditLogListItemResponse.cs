using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Audits
{
    public sealed class AuditLogListItemResponse
    {
        public Guid AuditId { get; set; }
        public Guid EventId { get; set; }
        public AuditCategory Category { get; set; }
        public string Action { get; set; } = string.Empty;
        public AuditOutcome Outcome { get; set; }
        public string? ReasonCode { get; set; }
        public AuditActorType ActorType { get; set; }
        public Guid? UserId { get; set; }
        public UserRole? UserRole { get; set; }
        public string? TargetType { get; set; }
        public Guid? TargetId { get; set; }
        public AuditSource Source { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public DateTime RecordedAtUtc { get; set; }
    }
}
