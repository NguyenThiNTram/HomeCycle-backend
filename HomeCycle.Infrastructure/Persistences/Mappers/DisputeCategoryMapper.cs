using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.Persistences.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class DisputeCategoryMapper
    {
        public static dispute_category ToDomain(this Dispute_Category entity)
        {
            return new dispute_category
            {
                DisputeCategoryId = entity.DisputeCategoryId,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                TargetTypes = entity.Targets.Select(x => (DisputeTargetType)x.TargetType).ToList()
            };
        }

        public static Dispute_Category ToInfrastructure(this dispute_category entity)
        {
            return new Dispute_Category
            {
                DisputeCategoryId = entity.DisputeCategoryId,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy
            };
        }
    }
}
