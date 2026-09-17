using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.SubscriptionPackages
{
    public class CreateSubscriptionPackageRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public UserRole TargetRole { get; set; }
        public List<PackageEntitlementRequest> Entitlements { get; set; } = new();
    }
}
