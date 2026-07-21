using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.Persistences.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BrandMapper
    {
        public static brand ToDomain(this Brand entity) 
        { 
            return new brand
            {
                BrandId = entity.BrandId,
                BrandName = entity.BrandName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }

        public static Brand ToInfrastructure(this brand entity) 
        { 
            return new Brand
            {
                BrandId = entity.BrandId,
                BrandName = entity.BrandName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
