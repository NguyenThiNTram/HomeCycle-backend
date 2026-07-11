using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class MediaMapper
    {
        public static media ToDomain(this Media entity)
        {
            return new Domain.Entities.media
            {
                MediaId = entity.MediaId,
                TargetId = entity.TargetId,
                TargetType = entity.TargetType,
                FileName = entity.FileName,
                FileSize = entity.FileSize,
                DisplayOrder = entity.DisplayOrder,
                Url = entity.Url,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            }; 
        }
        public static Media ToInfrastructure(this media entity)
        {
            return new Media
            {
                MediaId = entity.MediaId,
                TargetId = entity.TargetId,
                TargetType = entity.TargetType,
                FileName = entity.FileName,
                FileSize = entity.FileSize,
                DisplayOrder = entity.DisplayOrder,
                Url = entity.Url,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
