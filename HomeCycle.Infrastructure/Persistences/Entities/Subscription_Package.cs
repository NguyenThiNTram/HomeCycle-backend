using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Subscription_Package")]
[Index("Name", Name = "ux_subscription_package_name", IsUnique = true)]
public partial class Subscription_Package
{
    [Key]
    public Guid PackageId { get; set; }

    [StringLength(255)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Precision(18, 2)]
    public decimal? Price { get; set; }

    public int? Duration { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Package")]
    public virtual ICollection<User_Subscription> User_Subscriptions { get; set; } = new List<User_Subscription>();
}
