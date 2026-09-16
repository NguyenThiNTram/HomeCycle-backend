using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;
public class audit_log
{
    public Guid AuditId { get; set; }
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

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public AuditSource Source { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }

    public string? Metadata { get; set; }

    public audit_log()
    {
    }

    public audit_log(Guid AuditId, Guid EventId)
    {
        this.AuditId = AuditId;
        this.EventId = EventId;
    }
}
