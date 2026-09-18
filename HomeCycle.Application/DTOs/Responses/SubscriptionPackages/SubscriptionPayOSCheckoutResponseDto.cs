using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class SubscriptionPayOSCheckoutResponseDto
    {
        public string? PackageNameSnapshot { get; set; }
        public int? DurationDaysSnapshot { get; set; }
        public decimal? CheckoutAmount { get; set; }
        public Guid SubscriptionId { get; set; }
        public HomeCycle.Domain.Enums.UserSubscriptionStatus SubscriptionStatus { get; set; } = HomeCycle.Domain.Enums.UserSubscriptionStatus.Pending;
        public DateTime? ExpiresAt { get; set; }
        public IReadOnlyList<UserSubscriptionEntitlementResponseDto> Entitlements { get; set; } = Array.Empty<UserSubscriptionEntitlementResponseDto>();
        public Guid PaymentId { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public DateTime CheckoutExpiresAt { get; set; }
    }
}
