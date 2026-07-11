using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ReviewMapper
    {
        public static review ToDomain(this Review entity)
        {
            if (entity == null) return null;
            return new review
            {
                ReviewId = entity.ReviewId,
                OrderId = entity.OrderId,
                ReviewerId = entity.ReviewerId,
                RevieweeId = entity.RevieweeId,
                Rating = entity.Rating,
                Comment = entity.Comment,
                ReviewStatus = entity.ReviewStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Review ToInfrastructure(this review entity)
        {
            if (entity == null) return null;
            return new Review
            {
                ReviewId = entity.ReviewId,
                OrderId = entity.OrderId,
                ReviewerId = entity.ReviewerId,
                RevieweeId = entity.RevieweeId,
                Rating = entity.Rating,
                Comment = entity.Comment,
                ReviewStatus = entity.ReviewStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
