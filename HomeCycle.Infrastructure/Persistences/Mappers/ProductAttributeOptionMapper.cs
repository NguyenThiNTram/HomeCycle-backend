using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ProductAttributeOptionMapper
    {
        public static product_attribute_option ToDomain(this Product_Attribute_Option entity)
        {
            if (entity == null) return null;
            return new product_attribute_option
            {
                OptionId = entity.OptionId,
                AttributeId = entity.AttributeId,
                OptionValue = entity.OptionValue,
                DisplayOrder = entity.DisplayOrder,
                IsDefault = entity.IsDefault
            };
        }
        public static Product_Attribute_Option ToInfrastructure(this product_attribute_option entity)
        {
            if (entity == null) return null;
            return new Product_Attribute_Option
            {
                OptionId = entity.OptionId,
                AttributeId = entity.AttributeId,
                OptionValue = entity.OptionValue,
                DisplayOrder = entity.DisplayOrder,
                IsDefault = entity.IsDefault
            };
        }
    }
}
