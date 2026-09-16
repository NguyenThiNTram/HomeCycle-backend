using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Audit_Log")]
[Index("EventId", Name = "uq_audit_event_id", IsUnique = true)]
[Index("OccurredAtUtc", Name = "idx_audit_occurred")]
[Index("UserId", "OccurredAtUtc", Name = "idx_audit_user_occurred")]
[Index("TargetType", "TargetId", "OccurredAtUtc", Name = "idx_audit_target_occurred")]
[Index("Action", "OccurredAtUtc", Name = "idx_audit_action_occurred")]
public partial class Audit_Log
{
    [Key]
    public Guid AuditId { get; set; }

    public Guid EventId { get; set; }

    public int Category { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = null!;

    public int Outcome { get; set; }

    [StringLength(150)]
    public string? ReasonCode { get; set; }

    public int ActorType { get; set; }

    public Guid? UserId { get; set; }

    public int? UserRole { get; set; }

    [StringLength(100)]
    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public int Source { get; set; }

    [StringLength(100)]
    public string? CorrelationId { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(512)]
    public string? UserAgent { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public string? Metadata { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Audit_Logs")]
    public virtual User? User { get; set; }
}
