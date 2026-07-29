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
        // ============ KEYWORD ============
        /// Product.ProductName, Post.Description, Brand.BrandName, Post.City/Ward/StreetAddress
        public string? Keyword { get; set; }

        // ============ PHÂN LOẠI BÀI ĐĂNG — độc lập, không thuộc cụm filter nâng cao ============
        /// Lọc Bán/Mua — tương đương 2 tab riêng ở FE, không nằm trong luồng Category → Attribute
        public PostType? PostType { get; set; }

        // ============ FILTER TĨNH — phân cấp Category → Brand → ProductType ============
        public Guid? CategoryId { get; set; }
        public Guid? ProductTypeId { get; set; }
        public Guid? BrandId { get; set; }

        public string? SpaceUsage { get; set; }
        public FunctionalityStatus? FunctionalityStatus { get; set; }
        public int? MinUsageDuration { get; set; }
        public int? MaxUsageDuration { get; set; }
        public int? MinDamageLevel { get; set; }
        public int? MaxDamageLevel { get; set; }

        // ============ KHOẢNG GIÁ ============
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // ============ BASE FILTER — nâng cao trải nghiệm ============
        ///Chỉ hiện bài còn hàng (RemainingQuantity > 0). Mặc định true để tránh hiện hàng đã bán hết
        public bool OnlyAvailable { get; set; } = true;

        /// Đăng trong N ngày gần đây
        public int? PostedWithinDays { get; set; }

        public DeliveryMethod? DeliveryMethod { get; set; }

        public PriorityLevel? PriorityLevel { get; set; }

        public string? City { get; set; }
        public string? Ward { get; set; }

        // ============ SORT ============
        public PostSortBy SortBy { get; set; } = PostSortBy.Newest;

        // ============ DYNAMIC FILTER — AttributeFilters ============
        public List<AttributeFilterRequest> AttributeFilters { get; set; } = new();

    }
}

