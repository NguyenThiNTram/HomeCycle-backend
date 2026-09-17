using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Infrastructure.Persistences.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Subscription_Package")]
[Index("Code", Name = "ux_subscription_package_code", IsUnique = true)]
[Index("Name", Name = "ux_subscription_package_name", IsUnique = true)]
public partial class Subscription_Package
{
    [Key]
    public Guid PackageId { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Precision(18, 2)]
    public decimal Price { get; set; }

    public int Duration { get; set; }
    public int TargetRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Package")]
    public virtual ICollection<Subscription_Package_Entitlement> Subscription_Package_Entitlements { get; set; } =
        new List<Subscription_Package_Entitlement>();

    [InverseProperty("Package")]
    public virtual ICollection<User_Subscription> User_Subscriptions { get; set; } =
        new List<User_Subscription>();
}
