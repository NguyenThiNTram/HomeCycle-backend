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
                CategoryName = entity.Category?.CategoryName,
                ProductTypeName = entity.ProductType?.ProductTypeName,
                BrandName = entity.Brand?.BrandName,
                SpaceUsage = (SpaceUsage?)entity.SpaceUsage,
                ModelNumber = entity.ModelNumber,
                OriginalPrice = entity.OriginalPrice,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                Weight = entity.Weight,
                FunctionalityStatus = (FunctionalityStatus?)entity.FunctionalityStatus,
                UsageDuration = entity.UsageDuration,
                DamageLevel = (DamageLevel?)entity.DamageLevel,
                DetailDescription = entity.DetailDescription,
                Product_Attribute_Values = entity.Product_Attribute_Values?
                    .Select(x => x.ToDomain())
                    .ToList()
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
                SpaceUsage = (int?)entity.SpaceUsage,
                ModelNumber = entity.ModelNumber,
                OriginalPrice = entity.OriginalPrice,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                Weight = entity.Weight,
                FunctionalityStatus = (int?)entity.FunctionalityStatus,
                UsageDuration = entity.UsageDuration,
                DamageLevel = (int?)entity.DamageLevel,
                DetailDescription = entity.DetailDescription
            };
        }
    }
}
