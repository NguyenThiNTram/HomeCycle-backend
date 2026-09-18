using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class SubscriptionPackageMapper
    {
        public static subscription_package ToDomain(this Subscription_Package entity)
        {
            return new subscription_package
            {
                PackageId = entity.PackageId,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Duration = entity.Duration,
                TargetRole = (UserRole)entity.TargetRole,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Entitlements = entity.Subscription_Package_Entitlements
                    .Select(x => x.ToDomain())
                    .ToList()
            };
        }

        public static Subscription_Package ToInfrastructure(this subscription_package entity, bool includeEntitlements = true)
        {
            var result = new Subscription_Package
            {
                PackageId = entity.PackageId,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Duration = entity.Duration,
                TargetRole = (int)entity.TargetRole,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };

            if (includeEntitlements)
            {
                result.Subscription_Package_Entitlements = entity.Entitlements
                    .Select(x => x.ToInfrastructure())
                    .ToList();
            }

            return result;
        }
    }
}
