using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class SubscriptionPackageResponseDto
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
        public IReadOnlyList<PackageEntitlementResponseDto> Entitlements { get; set; } =
            Array.Empty<PackageEntitlementResponseDto>();
    }
}
