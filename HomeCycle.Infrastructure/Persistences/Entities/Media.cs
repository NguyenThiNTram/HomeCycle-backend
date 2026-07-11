using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Index("TargetType", "TargetId", Name = "idx_media_target")]
public partial class Media
{
    [Key]
    public Guid MediaId { get; set; }

    public Guid? TargetId { get; set; }

    [StringLength(50)]
    public string? TargetType { get; set; }

    [StringLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public int? DisplayOrder { get; set; }

    [StringLength(500)]
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
