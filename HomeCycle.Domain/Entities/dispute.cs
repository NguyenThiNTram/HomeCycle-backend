using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class dispute
{
    public Guid DisputeId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? TargetUserId { get; set; }
    public Guid? ModeratorId { get; set; }
    public Guid? ReviewId { get; set; }
    public Guid? OrderId { get; set; }

    public int? DisputeTargetType { get; set; }
    public int? DisputeCategory { get; set; }
    public string? Description { get; set; }
    public int? DisputeStatus { get; set; }
    public string? ModeratorNote { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public dispute()
    {
    }

    public dispute(Guid DisputeId, Guid SenderId)
    {
        this.DisputeId = DisputeId;
        this.SenderId = SenderId;
    }
}
