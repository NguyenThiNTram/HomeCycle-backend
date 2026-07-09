using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Dispute")]
[Index("ModeratorId", Name = "idx_dispute_moderator")]
[Index("OrderId", Name = "idx_dispute_order")]
[Index("OrderId", "DisputeStatus", Name = "idx_dispute_order_status")]
[Index("SenderId", Name = "idx_dispute_sender")]
[Index("DisputeStatus", Name = "idx_dispute_status")]
[Index("TargetUserId", Name = "idx_dispute_target_user")]
public partial class Dispute
{
    [Key]
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

    [ForeignKey("ModeratorId")]
    [InverseProperty("DisputeModerators")]
    public virtual User? Moderator { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Disputes")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ReviewId")]
    [InverseProperty("Disputes")]
    public virtual Review? Review { get; set; }

    [ForeignKey("SenderId")]
    [InverseProperty("DisputeSenders")]
    public virtual User Sender { get; set; } = null!;

    [ForeignKey("TargetUserId")]
    [InverseProperty("DisputeTargetUsers")]
    public virtual User? TargetUser { get; set; }
}
