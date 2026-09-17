using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class UserSubscriptionMapper
    {
        public static user_subscription ToDomain(this User_Subscription entity)
        {
            if (entity == null) return null;
            return new user_subscription
            {
                SubscriptionId = entity.SubscriptionId,
                UserId = entity.UserId,
                PackageId = entity.PackageId,
                PricePaid = entity.PricePaid,
                ActivatedAt = entity.ActivatedAt,
                ExpiresAt = entity.ExpiresAt,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                Entitlements = entity.User_Subscription_Entitlements
                    .Select(x => x.ToDomain())
                    .ToList()
            };
        }
        public static User_Subscription ToInfrastructure(this user_subscription entity)
        {
            var result = new User_Subscription
            {
                SubscriptionId = entity.SubscriptionId,
                UserId = entity.UserId,
                PackageId = entity.PackageId,
                PricePaid = entity.PricePaid,
                ActivatedAt = entity.ActivatedAt,
                ExpiresAt = entity.ExpiresAt,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            };

            result.User_Subscription_Entitlements = entity.Entitlements
                .Select(x => x.ToInfrastructure())
                .ToList();

            return result;
        }
    }
}
