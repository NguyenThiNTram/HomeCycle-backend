using HomeCycle.Application.DTOs.Responses.Media;
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

        public string? AvatarUrl { get; set; }

        public VerifyStatus? VerifyStatus { get; set; }

        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string OwnerName { get; set; }

        public double? AverageRating { get; set; }

        public int TotalReviews { get; set; }

        public string? ProductTypeName { get; set; }

        public string? CategoryName { get; set; }

        public string? BrandName { get; set; }

        public string? Description { get; set; }

        public int? Quantity { get; set; }

        public int? RemainingQuantity { get; set; }

        public PostType? PostType { get; set; }

        public decimal? BasePrice { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;

        public DeliveryMethod? DeliveryMethod { get; set; }

        public string? PriorityLevel { get; set; }

        public PostStatus? Status { get; set; }

        public string? StreetAddress { get; set; }

        public string? Ward { get; set; }

        public string? City { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public IReadOnlyList<MediaResponse> Medias { get; set; }
        = Array.Empty<MediaResponse>();
    }

    public abstract class TypedPostDetailResponse
    {
        protected TypedPostDetailResponse(PostDetailResponse source)
        {
            PostId = source.PostId;
            OwnerId = source.OwnerId;
            AvatarUrl = source.AvatarUrl;
            VerifyStatus = source.VerifyStatus;
            OwnerName = source.OwnerName;
            AverageRating = source.AverageRating;
            TotalReviews = source.TotalReviews;
            Description = source.Description;
            Quantity = source.Quantity;
            PostType = source.PostType;
            Status = source.Status;
            StreetAddress = source.StreetAddress;
            Ward = source.Ward;
            City = source.City;
            CreatedAt = source.CreatedAt;
            UpdatedAt = source.UpdatedAt;
            ExpiryDate = source.ExpiryDate;
            IsExpired = source.IsExpired;
            Medias = source.Medias;
        }

        public Guid PostId { get; }
        public Guid OwnerId { get; }
        public string? AvatarUrl { get; }
        public VerifyStatus? VerifyStatus { get; }
        public string OwnerName { get; }
        public double? AverageRating { get; }
        public int TotalReviews { get; }
        public string? Description { get; }
        public int? Quantity { get; }
        public PostType? PostType { get; }
        public PostStatus? Status { get; }
        public string? StreetAddress { get; }
        public string? Ward { get; }
        public string? City { get; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
        public DateTime? ExpiryDate { get; }
        public bool IsExpired { get; }
        public IReadOnlyList<MediaResponse> Medias { get; }
    }

    public sealed class SellPostDetailResponse : TypedPostDetailResponse
    {
        public SellPostDetailResponse(PostDetailResponse source) : base(source)
        {
            Product = source.Product;
            BasePrice = source.BasePrice;
            RemainingQuantity = source.RemainingQuantity;
            DeliveryMethod = source.DeliveryMethod;
            PriorityLevel = source.PriorityLevel;
        }

        public ProductResponse Product { get; }
        public decimal? BasePrice { get; }
        public int? RemainingQuantity { get; }
        public DeliveryMethod? DeliveryMethod { get; }
        public string? PriorityLevel { get; }
    }

    public sealed class BuyPostDetailResponse : TypedPostDetailResponse
    {
        public BuyPostDetailResponse(PostDetailResponse source, int agreedQuantity) : base(source)
        {
            Requirement = new BuyProductRequirementResponse(source.Product);
            PriceFrom = source.PriceFrom;
            PriceTo = source.PriceTo;
            PriorityLevel = source.PriorityLevel;
            Progress = new BuyPostProgressResponse(source.Quantity ?? 0, agreedQuantity);
        }

        public BuyProductRequirementResponse Requirement { get; }
        public decimal? PriceFrom { get; }
        public decimal? PriceTo { get; }
        public string? PriorityLevel { get; }
        public BuyPostProgressResponse Progress { get; }
    }

    public sealed record BuyPostProgressResponse(int TargetQuantity, int AgreedQuantity)
    {
        public int RemainingTargetQuantity => Math.Max(0, TargetQuantity - AgreedQuantity);
        public decimal ProgressPercent => TargetQuantity > 0
            ? Math.Min(100m, Math.Round(AgreedQuantity * 100m / TargetQuantity, 2)) : 0;
        public bool IsTargetReached => TargetQuantity > 0 && AgreedQuantity >= TargetQuantity;
    }

    public class PostDetailResponse : PostResponse
    {
        public ProductResponse Product { get; set; } = null!;

        //public IReadOnlyList<ProductAttributeValueResponse> AttributeValues { get; set; } = new List<ProductAttributeValueResponse>();

        public IReadOnlyList<MediaResponse> Medias { get; set; } = new List<MediaResponse>();
    }
}
