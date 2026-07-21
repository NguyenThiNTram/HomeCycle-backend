using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ProductAttributeValueMapper
    {
        public static product_attribute_value ToDomain(this Product_Attribute_Value entity)
        {
            if (entity == null) return null;
            return new product_attribute_value
            {
                ProductId = entity.ProductId,
                AttributeId = entity.AttributeId,
                OptionId = entity.OptionId,
                InputType = (InputType?)entity.InputType,
                ValueBoolean = entity.ValueBoolean,
                ValueText = entity.ValueText,
                ValueNumber = entity.ValueNumber
            };
        }
        public static Product_Attribute_Value ToInfrastructure(this product_attribute_value entity)
        {
            if (entity == null) return null;
            return new Product_Attribute_Value
            {
                ProductId = entity.ProductId,
                AttributeId = entity.AttributeId,
                OptionId = entity.OptionId,
                InputType = (int?)entity.InputType,
                ValueBoolean = entity.ValueBoolean,
                ValueText = entity.ValueText,
                ValueNumber = entity.ValueNumber
            };
        }
    }
}
