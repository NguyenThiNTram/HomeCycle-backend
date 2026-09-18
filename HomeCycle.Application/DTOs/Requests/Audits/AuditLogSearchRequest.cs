using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Audits
{
    public sealed class AuditLogSearchRequest : PaginationRequest
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public AuditCategory? Category { get; set; }
        public string? Action { get; set; }
        public AuditOutcome? Outcome { get; set; }
        public AuditActorType? ActorType { get; set; }
        public Guid? UserId { get; set; }
        public UserRole? UserRole { get; set; }
        public string? TargetType { get; set; }
        public Guid? TargetId { get; set; }
        public AuditSource? Source { get; set; }
        public string? CorrelationId { get; set; }
    }
}
