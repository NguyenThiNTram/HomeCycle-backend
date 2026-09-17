using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.SubscriptionPackages
{
    public class PackageEntitlementRequest
    {
        public string Key { get; set; } = string.Empty;
        public decimal? NumericValue { get; set; }
        public bool? BooleanValue { get; set; }
        public bool IsUnlimited { get; set; }
    }
}
