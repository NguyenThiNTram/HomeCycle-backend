using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;
public class audit_log
{
    public Guid AuditId { get; set; }
    public Guid? UserId { get; set; }

    public int? UserRole { get; set; }

    public int? Action { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public string? TargetTable { get; set; }
    public Guid? TargetId { get; set; }

    public DateTime CreatedAt { get; set; }

    public audit_log()
    {
    }

    public audit_log(Guid AuditId)
    {
        this.AuditId = AuditId;
    }

}
