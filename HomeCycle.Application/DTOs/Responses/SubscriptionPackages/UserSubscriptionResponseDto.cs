using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class UserSubscriptionResponseDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public Guid PackageId { get; set; }
        public decimal? PricePaid { get; set; }
        public UserSubscriptionStatus Status { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<UserSubscriptionEntitlementResponseDto> Entitlements { get; set; } =
            Array.Empty<UserSubscriptionEntitlementResponseDto>();
    }
}
