using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class subscription_package
{
    public Guid PackageId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Duration { get; set; }
    public UserRole TargetRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<subscription_package_entitlement> Entitlements { get; set; } = new();

    public subscription_package()
    {
    }

    public subscription_package(Guid packageId)
    {
        PackageId = packageId;
    }
}
