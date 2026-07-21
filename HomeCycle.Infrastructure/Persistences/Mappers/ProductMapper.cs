using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ProductMapper
    {
        public static product ToDomain(this Product entity)
        {
            return new product
            {
                ProductId = entity.ProductId,
                PostId = entity.PostId,
                CategoryId = entity.CategoryId,
                BrandId = entity.BrandId,
                ProductTypeId = entity.ProductTypeId,
                ProductName = entity.ProductName,
                SpaceUsage = entity.SpaceUsage,
                ModelNumber = entity.ModelNumber,
                OriginalPrice = entity.OriginalPrice,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                Weight = entity.Weight,
                FunctionalityStatus = (FunctionalityStatus?)entity.FunctionalityStatus,
                UsageDuration = entity.UsageDuration,
                DamageLevel = entity.DamageLevel,
                DetailDescription = entity.DetailDescription
            };
        }
        public static Product ToInfrastructure(this product entity)
        {
            return new Product
            {
                ProductId = entity.ProductId,
                PostId = entity.PostId,
                CategoryId = entity.CategoryId,
                ProductTypeId = entity.ProductTypeId,
                BrandId = entity.BrandId,
                ProductName = entity.ProductName,
                SpaceUsage = entity.SpaceUsage,
                ModelNumber = entity.ModelNumber,
                OriginalPrice = entity.OriginalPrice,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                Weight = entity.Weight,
                FunctionalityStatus = (int?)entity.FunctionalityStatus,
                UsageDuration = entity.UsageDuration,
                DamageLevel = entity.DamageLevel,
                DetailDescription = entity.DetailDescription
            };
        }
    }
}
