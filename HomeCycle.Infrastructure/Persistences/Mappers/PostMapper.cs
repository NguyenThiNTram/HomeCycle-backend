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

            var productDomain = entity.Product?.ToDomain();
            var userDomain = entity.User?.ToDomain();

            return new post
            {
                PostId = entity.PostId,
                OwnerId = entity.OwnerId,
                //ProductName = entity.Product?.ProductName,
                Description = entity.Description,
                Quantity = entity.Quantity,
                RemainingQuantity = entity.RemainingQuantity,
                PostType = (PostType?)entity.PostType,
                BasePrice = entity.BasePrice,
                StreetAddress = entity.StreetAddress,
                Ward = entity.Ward,
                City = entity.City,
                DeliveryMethod = entity.DeliveryMethod,
                PriorityLevel = (PriorityLevel?)entity.PriorityLevel,
                Status = (PostStatus?)entity.Status,
                IsBusinessPosting = entity.IsBusinessPosting,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ExpiryDate = entity.ExpiryDate,

                Product = productDomain,
                User = userDomain

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
                PriorityLevel = (int?)entity.PriorityLevel,
                Status = (int?)entity.Status,
                IsBusinessPosting = entity.IsBusinessPosting,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ExpiryDate = entity.ExpiryDate
            };
        }
    }
}
