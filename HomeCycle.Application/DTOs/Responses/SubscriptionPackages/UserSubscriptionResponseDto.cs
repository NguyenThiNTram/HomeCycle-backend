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
        public string? PackageNameSnapshot { get; set; }
        public int? DurationDaysSnapshot { get; set; }
        public decimal? CheckoutAmount { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public Guid PackageId { get; set; }
        public DateTime? CheckoutExpiresAt { get; set; }
        public HomeCycle.Application.DTOs.Responses.Wallets.WithdrawalQuotaResponseDto? WithdrawalQuota { get; set; }
        public int? AiDailyLimit { get; set; }
        public int? AiRemainingToday { get; set; }
        public DateTimeOffset? AiResetsAt { get; set; }
        public decimal? PricePaid { get; set; }
        public UserSubscriptionStatus Status { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<UserSubscriptionEntitlementResponseDto> Entitlements { get; set; } =
            Array.Empty<UserSubscriptionEntitlementResponseDto>();
    }
}
