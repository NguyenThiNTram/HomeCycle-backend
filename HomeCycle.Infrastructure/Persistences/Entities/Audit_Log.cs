using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Audit_Log")]
[Index("CreatedAt", Name = "idx_audit_created")]
[Index("TargetTable", "TargetId", Name = "idx_audit_target")]
[Index("UserId", Name = "idx_audit_user")]
public partial class Audit_Log
{
    [Key]
    public Guid AuditId { get; set; }

    public Guid? UserId { get; set; }

    public int? UserRole { get; set; }

    public int? Action { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [StringLength(100)]
    public string? TargetTable { get; set; }

    public Guid? TargetId { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Audit_Logs")]
    public virtual User? User { get; set; }
}
