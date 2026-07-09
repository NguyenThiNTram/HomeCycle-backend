using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Notification")]
[Index("IsRead", Name = "idx_notification_read")]
[Index("TargetType", "TargetId", Name = "idx_notification_target")]
[Index("UserId", Name = "idx_notification_user")]
public partial class Notification
{
    [Key]
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Message { get; set; }

    public int? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
