using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class PostSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }

        public PostType? PostType { get; set; }

        public Guid? CategoryId { get; set; }
        public Guid? ProductTypeId { get; set; }
        public Guid? BrandId { get; set; }

        public SpaceUsage? SpaceUsage { get; set; }
        public FunctionalityStatus? FunctionalityStatus { get; set; }
        public int? MinUsageDuration { get; set; }
        public int? MaxUsageDuration { get; set; }
        public int? MinDamageLevel { get; set; }
        public int? MaxDamageLevel { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public bool? OnlyAvailable { get; set; } = true;

        /// Đăng trong N ngày gần đây
        public int? PostedWithinDays { get; set; }

        public DeliveryMethod? DeliveryMethod { get; set; }

        public PriorityLevel? PriorityLevel { get; set; }

        public string? City { get; set; }
        public string? Ward { get; set; }

        // ============ SORT ============
        public PostSortBy? SortBy { get; set; } = PostSortBy.Newest;

        // ============ DYNAMIC FILTER — AttributeFilters ============
        public List<AttributeFilterRequest>? AttributeFilters { get; set; } = new();

    }
}

