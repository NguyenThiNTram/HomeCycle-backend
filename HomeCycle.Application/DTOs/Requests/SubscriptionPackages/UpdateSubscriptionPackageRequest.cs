using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.SubscriptionPackages
{
    public class UpdateSubscriptionPackageRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? Duration { get; set; }
        public List<PackageEntitlementRequest>? Entitlements { get; set; }
    }
}
