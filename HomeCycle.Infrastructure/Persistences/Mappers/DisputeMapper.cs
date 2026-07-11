using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class DisputeMapper
    {
        public static dispute ToDomain(this Dispute entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return new dispute
            {
                DisputeId = entity.DisputeId,
                SenderId = entity.SenderId,
                TargetUserId = entity.TargetUserId,
                ModeratorId = entity.ModeratorId,
                ReviewId = entity.ReviewId,
                OrderId = entity.OrderId,
                DisputeTargetType = entity.DisputeTargetType,
                DisputeCategory = entity.DisputeCategory,
                Description = entity.Description,
                DisputeStatus = entity.DisputeStatus,
                ModeratorNote = entity.ModeratorNote,
                CreatedAt = entity.CreatedAt,
                ResolvedAt = entity.ResolvedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Dispute ToInfrastructure(this dispute entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return new Dispute
            {
                DisputeId = entity.DisputeId,
                SenderId = entity.SenderId,
                TargetUserId = entity.TargetUserId,
                ModeratorId = entity.ModeratorId,
                ReviewId = entity.ReviewId,
                OrderId = entity.OrderId,
                DisputeTargetType = entity.DisputeTargetType,
                DisputeCategory = entity.DisputeCategory,
                Description = entity.Description,
                DisputeStatus = entity.DisputeStatus,
                ModeratorNote = entity.ModeratorNote,
                CreatedAt = entity.CreatedAt,
                ResolvedAt = entity.ResolvedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
