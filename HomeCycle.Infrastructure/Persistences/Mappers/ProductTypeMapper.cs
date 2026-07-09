using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ProductTypeMapper
    {
        public static product_type ToDomain(this Product_Type entity)
        {
            if (entity == null) return null;
            return new product_type
            {
                ProductTypeId = entity.ProductTypeId,
                CategoryId = entity.CategoryId,
                ProductTypeName = entity.ProductTypeName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Product_Type ToInfrastructure(this product_type entity)
        {
            if (entity == null) return null;
            return new Product_Type
            {
                ProductTypeId = entity.ProductTypeId,
                CategoryId = entity.CategoryId,
                ProductTypeName = entity.ProductTypeName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
