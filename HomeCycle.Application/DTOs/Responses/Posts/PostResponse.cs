using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Posts
{
    public class PostResponse
    {
        public Guid PostId { get; set; }

        public Guid OwnerId { get; set; }

        public string? Description { get; set; }

        public int Quantity { get; set; }

        public int RemainingQuantity { get; set; }

        public PostType? PostType { get; set; }

        public decimal? BasePrice { get; set; }

        public DeliveryMethod? DeliveryMethod { get; set; }

        public string? PriorityLevel { get; set; }

        public PostStatus? Status { get; set; }

        public bool IsBusinessPosting { get; set; }

        public string? StreetAddress { get; set; }

        public string? Ward { get; set; }

        public string? City { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    public class PostDetailResponse : PostResponse
    {
        public ProductResponse Product { get; set; } = null!;

        //public IReadOnlyList<ProductAttributeValueResponse> AttributeValues { get; set; } = new List<ProductAttributeValueResponse>();

        public IReadOnlyList<MediaResponse> Medias { get; set; } = new List<MediaResponse>();
    }
}
