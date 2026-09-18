using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Entities
{
    public class user_subscription_entitlement
    {
        public Guid SubscriptionEntitlementId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string EntitlementKey { get; set; } = string.Empty;
        public EntitlementValueType ValueType { get; set; }
        public decimal? NumericValue { get; set; }
        public bool? BooleanValue { get; set; }
        public bool IsUnlimited { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
