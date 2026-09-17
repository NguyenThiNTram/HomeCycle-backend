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
    [Table("Subscription_Package_Entitlement")]
    [Index("PackageId", "EntitlementKey", Name = "ux_subscription_package_entitlement_package_key", IsUnique = true)]
    public partial class Subscription_Package_Entitlement
    {
        [Key]
        public Guid PackageEntitlementId { get; set; }

        public Guid PackageId { get; set; }

        [StringLength(100)]
        public string EntitlementKey { get; set; } = string.Empty;

        public int ValueType { get; set; }

        [Precision(18, 2)]
        public decimal? NumericValue { get; set; }

        public bool? BooleanValue { get; set; }
        public bool IsUnlimited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("PackageId")]
        [InverseProperty("Subscription_Package_Entitlements")]
        public virtual Subscription_Package Package { get; set; } = null!;
    }
}
