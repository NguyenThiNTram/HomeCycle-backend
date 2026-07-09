using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("User_Subscription")]
[Index("PackageId", Name = "idx_user_subscription_package")]
[Index("UserId", Name = "idx_user_subscription_user")]
public partial class User_Subscription
{
    [Key]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    public Guid PackageId { get; set; }

    [Precision(18, 2)]
    public decimal? PricePaid { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("PackageId")]
    [InverseProperty("User_Subscriptions")]
    public virtual Subscription_Package Package { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("User_Subscriptions")]
    public virtual User User { get; set; } = null!;
}
