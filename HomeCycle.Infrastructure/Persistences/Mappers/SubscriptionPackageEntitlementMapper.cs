using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.Persistences.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class SubscriptionPackageEntitlementMapper
    {
        public static subscription_package_entitlement ToDomain(this Subscription_Package_Entitlement entity)
        {
            return new subscription_package_entitlement
            {
                PackageEntitlementId = entity.PackageEntitlementId,
                PackageId = entity.PackageId,
                EntitlementKey = entity.EntitlementKey,
                ValueType = (EntitlementValueType)entity.ValueType,
                NumericValue = entity.NumericValue,
                BooleanValue = entity.BooleanValue,
                IsUnlimited = entity.IsUnlimited,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static Subscription_Package_Entitlement ToInfrastructure(this subscription_package_entitlement entity)
        {
            return new Subscription_Package_Entitlement
            {
                PackageEntitlementId = entity.PackageEntitlementId,
                PackageId = entity.PackageId,
                EntitlementKey = entity.EntitlementKey,
                ValueType = (int)entity.ValueType,
                NumericValue = entity.NumericValue,
                BooleanValue = entity.BooleanValue,
                IsUnlimited = entity.IsUnlimited,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
