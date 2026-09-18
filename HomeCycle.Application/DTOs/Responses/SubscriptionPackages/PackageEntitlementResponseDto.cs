using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class PackageEntitlementResponseDto
    {
        public Guid PackageEntitlementId { get; set; }
        public string Key { get; set; } = string.Empty;
        public EntitlementValueType ValueType { get; set; }
        public decimal? NumericValue { get; set; }
        public bool? BooleanValue { get; set; }
        public bool IsUnlimited { get; set; }
    }
}
