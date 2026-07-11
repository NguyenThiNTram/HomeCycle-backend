using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BusinessServiceAreaMapper
    {
        public static business_service_area ToDomain(this Business_Service_Area entity)
        {
            return new business_service_area
            {
                BusinessServiceAreaId = entity.BusinessServiceAreaId,
                BusinessProfileId = entity.BusinessProfileId,
                City = entity.City,
                District = entity.District,
                Ward = entity.Ward,
                Priority = entity.Priority,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Business_Service_Area ToInfrastructure(this business_service_area entity)
        {
            return new Business_Service_Area
            {
                BusinessServiceAreaId = entity.BusinessServiceAreaId,
                BusinessProfileId = entity.BusinessProfileId,
                City = entity.City,
                District = entity.District,
                Ward = entity.Ward,
                Priority = entity.Priority,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
