using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Domain.Entities;

public partial class media
{
    public Guid MediaId { get; set; }
    public Guid? TargetId { get; set; }

    public string? TargetType { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public media()
    {
    }

    public media(Guid MediaId)
    {
        this.MediaId = MediaId;
    }
}
