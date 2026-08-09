using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class AgreementProductSnapshot
    {
        public PostSnapshotInfo PostInfo { get; set; } = null!;
        public ProductSnapshotInfo ProductInfo { get; set; } = null!;
        public IReadOnlyList<PostMediaSnapshotInfo> Medias { get; init; }
        = Array.Empty<PostMediaSnapshotInfo>();
    }

    public class PostSnapshotInfo
    {
        public Guid PostId { get; set; }
        public Guid OwnerId { get; set; }
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public PostType PostType { get; set; }
        public int PostedQuantity { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    public class ProductSnapshotInfo
    {
        public Guid ProductId { get; init; }

        public Guid CategoryId { get; init; }
        public string? CategoryName { get; init; }

        public Guid ProductTypeId { get; init; }
        public string? ProductTypeName { get; init; }

        public Guid? BrandId { get; init; }
        public string? BrandName { get; init; }

        public string? ProductName { get; init; }
        public string? ModelNumber { get; init; }
        public decimal? OriginalPrice { get; init; }

        public SpaceUsage? SpaceUsage { get; init; }
        public FunctionalityStatus? FunctionalityStatus { get; init; }
        public int? DamageLevel { get; init; }
        public int? UsageDuration { get; init; }

        //public decimal? Weight { get; set; }
        //public decimal? Length { get; set; }
        //public decimal? Width { get; set; }
        //public decimal? Height { get; set; }

        public ProductMeasurementSnapshotInfo? Measurements { get; init; }

        public IReadOnlyList<ProductAttributeSnapshotInfo> Attributes { get; init; }
            = Array.Empty<ProductAttributeSnapshotInfo>();
    }

    public sealed class ProductMeasurementSnapshotInfo
    {
        // Giá trị đã chuẩn hóa khi tạo snapshot
        public decimal? Weight { get; init; }

        public decimal? Length { get; init; }
        public decimal? Width { get; init; }
        public decimal? Height { get; init; }
    }

    public sealed class ProductAttributeSnapshotInfo
    {
        public Guid AttributeId { get; init; }
        public string AttributeName { get; init; }

        public DataType DataType { get; init; }
        public InputMode InputMode { get; init; }

        public string? Unit { get; init; }

        public Guid? OptionId { get; init; }
        public string? OptionLabel { get; init; }

        public bool? ValueBoolean { get; init; }
        public decimal? ValueNumber { get; init; }
        public string? ValueText { get; init; }

        // Giá trị đã định dạng để hiển thị lịch sử
        public string? DisplayValue { get; init; }
    }

    public sealed class PostMediaSnapshotInfo
    {
        public Guid MediaId { get; init; }

        public required string Url { get; init; }
        public string? FileName { get; init; }

        public long? FileSize { get; init; }
        public int DisplayOrder { get; init; }

        //// Hướng mở rộng để phát hiện file bị thay nội dung
        //public string? Sha256 { get; init; }
    }
}
