using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class SubscriptionPaymentStatusResponseDto
    {
        public string? PackageNameSnapshot { get; set; }
        public int? DurationDaysSnapshot { get; set; }
        public decimal? CheckoutAmount { get; set; }
        public Guid SubscriptionId { get; set; }
        public DateTime? CheckoutExpiresAt { get; set; }
        public IReadOnlyList<UserSubscriptionEntitlementResponseDto> Entitlements { get; set; } = Array.Empty<UserSubscriptionEntitlementResponseDto>();
        public Guid PaymentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public UserSubscriptionStatus SubscriptionStatus { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
