using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class user_subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public Guid PackageId { get; set; }
    public string? PackageNameSnapshot { get; set; }
    public int? DurationDaysSnapshot { get; set; }

    public decimal? PricePaid { get; set; }

    public int? Status { get; set; }

    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<user_subscription_entitlement> Entitlements { get; set; } = new();
    public user_subscription()
    {
    }

    public user_subscription(Guid SubscriptionId, Guid UserId, Guid PackageId)
    {
        this.SubscriptionId = SubscriptionId;
        this.UserId = UserId;
        this.PackageId = PackageId;
    }
}
