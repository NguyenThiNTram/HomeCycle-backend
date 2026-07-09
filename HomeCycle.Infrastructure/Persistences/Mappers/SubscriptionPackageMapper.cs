using HomeCycle.Domain.Entities;
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
            if (entity == null) return null;
            return new subscription_package
            {
                PackageId = entity.PackageId,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Duration = entity.Duration,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Subscription_Package ToInfrastructure(this subscription_package entity)
        {
            if (entity == null) return null;
            return new Subscription_Package
            {
                PackageId = entity.PackageId,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Duration = entity.Duration,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
