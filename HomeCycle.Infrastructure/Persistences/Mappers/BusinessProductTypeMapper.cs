using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BusinessProductTypeMapper
    {
        public static business_product_type ToDomain(this Business_Product_Type entity)
        {
            return new business_product_type
            {
                BusinessProductTypeId = entity.BusinessProductTypeId,
                BusinessProfileId = entity.BusinessProfileId,
                ProductTypeId = entity.ProductTypeId,
                Priority = entity.Priority,
                CreatedAt = entity.CreatedAt
            };
        }

        public static Business_Product_Type ToInfrastructure(this business_product_type entity)
        {
            return new Business_Product_Type
            {
                BusinessProductTypeId = entity.BusinessProductTypeId,
                BusinessProfileId = entity.BusinessProfileId,
                ProductTypeId = entity.ProductTypeId,
                Priority = entity.Priority,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
