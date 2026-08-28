using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Platform_Policy")]
[Index(nameof(PolicyType), nameof(Version), Name = "uq_platform_policy_type_version", IsUnique = true)]
public partial class Platform_Policy
{
    [Key]
    public Guid PolicyId { get; set; }

    [StringLength(50)]
    public string PolicyType { get; set; } = string.Empty;


    [StringLength(255)]
    public string? Title { get; set; }

    [Column(TypeName = "jsonb")]
    public string Content { get; set; } = "{}";

    public int Version { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }
}
