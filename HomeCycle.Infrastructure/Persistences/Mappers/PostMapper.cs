using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class PostMapper
    {
        public static post ToDomain(this Post entity)
        {
            if (entity == null) return null;
            return new post
            {
                PostId = entity.PostId,
                OwnerId = entity.OwnerId,
                Description = entity.Description,
                Quantity = entity.Quantity,
                RemainingQuantity = entity.RemainingQuantity,
                PostType = (PostType?)entity.PostType,
                BasePrice = entity.BasePrice,
                StreetAddress = entity.StreetAddress,
                Ward = entity.Ward,
                City = entity.City,
                DeliveryMethod = entity.DeliveryMethod,
                PriorityLevel = entity.PriorityLevel,
                Status = entity.Status,
                IsBusinessPosting = entity.IsBusinessPosting,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ExpiryDate = entity.ExpiryDate
            };
        }
        public static Post ToInfrastructure(this post entity)
        {
            if (entity == null) return null;
            return new Post
            {
                PostId = entity.PostId,
                OwnerId = entity.OwnerId,
                Description = entity.Description,
                Quantity = entity.Quantity,
                RemainingQuantity = entity.RemainingQuantity,
                PostType = (int?)entity.PostType,
                BasePrice = entity.BasePrice,
                StreetAddress = entity.StreetAddress,
                Ward = entity.Ward,
                City = entity.City,
                DeliveryMethod = entity.DeliveryMethod,
                PriorityLevel = entity.PriorityLevel,
                Status = entity.Status,
                IsBusinessPosting = entity.IsBusinessPosting,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ExpiryDate = entity.ExpiryDate
            };
        }
    }
}
