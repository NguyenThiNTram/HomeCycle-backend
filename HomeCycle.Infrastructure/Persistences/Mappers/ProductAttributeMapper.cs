using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ProductAttributeMapper
    {
        public static product_attribute ToDomain(this Product_Attribute entity)
        {
            if (entity == null) return null;
            return new product_attribute
            {
                AttributeId = entity.AttributeId,
                ProductTypeId = entity.ProductTypeId,
                AttributeName = entity.AttributeName,
                DataType = entity.DataType,
                Unit = entity.Unit,
                DisplayOrder = entity.DisplayOrder,
                IsFilterable = entity.IsFilterable,
                IsRequired = entity.IsRequired,

                ProductAttributeOptions = entity.Product_Attribute_Options?
                    .Select(opt => opt.ToDomain())
                    .ToList() ?? new List<product_attribute_option>()
            };
        }
        public static Product_Attribute ToInfrastructure(this product_attribute entity)
        {
            if (entity == null) return null;
            return new Product_Attribute
            {
                AttributeId = entity.AttributeId,
                ProductTypeId = entity.ProductTypeId,
                AttributeName = entity.AttributeName,
                DataType = entity.DataType,
                Unit = entity.Unit,
                DisplayOrder = entity.DisplayOrder,
                IsFilterable = entity.IsFilterable,
                IsRequired = entity.IsRequired,

                Product_Attribute_Options = entity.ProductAttributeOptions?
                    .Select(opt => opt.ToInfrastructure())
                    .ToList() ?? new List<Product_Attribute_Option>()
            };
        }
    }
}
