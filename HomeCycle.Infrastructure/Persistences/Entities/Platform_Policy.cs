using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Platform_Policy")]
[Index("PolicyType", "Version", Name = "uq_policy_type_version", IsUnique = true)]
[Index("PolicyType", "Version", Name = "ux_policy_type_version", IsUnique = true)]
public partial class Platform_Policy
{
    [Key]
    public Guid PolicyId { get; set; }

    [StringLength(50)]
    public string? PolicyType { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }
}
