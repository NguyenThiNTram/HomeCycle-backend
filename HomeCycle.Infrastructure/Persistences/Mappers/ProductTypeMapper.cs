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
                CreatedAt = entity.CreatedAt,

                ProductAttributes = entity.Product_Attributes?
                    .Select(attr => attr.ToDomain()) 
                    .ToList() ?? new List<product_attribute>()
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
                CreatedAt = entity.CreatedAt,
                Product_Attributes = entity.ProductAttributes.Select(attr => new Product_Attribute
                {
                    AttributeId = attr.AttributeId,
                    ProductTypeId = attr.ProductTypeId,
                    AttributeName = attr.AttributeName,
                    DataType = attr.DataType,
                    Unit = attr.Unit,
                    DisplayOrder = attr.DisplayOrder,
                    IsFilterable = attr.IsFilterable,
                    IsRequired = attr.IsRequired,
                    Product_Attribute_Options = attr.ProductAttributeOptions.Select(opt => new Product_Attribute_Option
                    {
                        OptionId = opt.OptionId,
                        AttributeId = opt.AttributeId,
                        OptionValue = opt.OptionValue,
                        DisplayOrder = opt.DisplayOrder,
                        IsDefault = opt.IsDefault
                    }).ToList()
                }).ToList()
            };
        }
    }
}
