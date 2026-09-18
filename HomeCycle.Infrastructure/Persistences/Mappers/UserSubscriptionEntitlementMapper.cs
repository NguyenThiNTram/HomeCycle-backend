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
    public static class UserSubscriptionEntitlementMapper
    {
        public static user_subscription_entitlement ToDomain(this User_Subscription_Entitlement entity)
        {
            return new user_subscription_entitlement
            {
                SubscriptionEntitlementId = entity.SubscriptionEntitlementId,
                SubscriptionId = entity.SubscriptionId,
                EntitlementKey = entity.EntitlementKey,
                ValueType = (EntitlementValueType)entity.ValueType,
                NumericValue = entity.NumericValue,
                BooleanValue = entity.BooleanValue,
                IsUnlimited = entity.IsUnlimited,
                CreatedAt = entity.CreatedAt
            };
        }

        public static User_Subscription_Entitlement ToInfrastructure(this user_subscription_entitlement entity)
        {
            return new User_Subscription_Entitlement
            {
                SubscriptionEntitlementId = entity.SubscriptionEntitlementId,
                SubscriptionId = entity.SubscriptionId,
                EntitlementKey = entity.EntitlementKey,
                ValueType = (int)entity.ValueType,
                NumericValue = entity.NumericValue,
                BooleanValue = entity.BooleanValue,
                IsUnlimited = entity.IsUnlimited,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
