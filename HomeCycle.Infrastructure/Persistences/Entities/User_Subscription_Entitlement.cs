using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("User_Subscription_Entitlement")]
    [Index("SubscriptionId", "EntitlementKey", Name = "ux_user_subscription_entitlement_subscription_key", IsUnique = true)]
    public partial class User_Subscription_Entitlement
    {
        [Key]
        public Guid SubscriptionEntitlementId { get; set; }

        public Guid SubscriptionId { get; set; }

        [StringLength(100)]
        public string EntitlementKey { get; set; } = string.Empty;

        public int ValueType { get; set; }

        [Precision(18, 2)]
        public decimal? NumericValue { get; set; }

        public bool? BooleanValue { get; set; }
        public bool IsUnlimited { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("SubscriptionId")]
        [InverseProperty("User_Subscription_Entitlements")]
        public virtual User_Subscription Subscription { get; set; } = null!;
    }
}
