using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class SubscriptionPayOSCheckoutResponseDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid PaymentId { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public DateTime CheckoutExpiresAt { get; set; }
    }
}
