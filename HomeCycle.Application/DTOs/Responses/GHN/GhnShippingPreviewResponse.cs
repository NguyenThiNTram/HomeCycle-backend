using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    // Kết quả gọi GHN v2/shipping-order/preview
    public sealed record GhnPreviewQuote(decimal TotalFee, DateTimeOffset? ExpectedDeliveryAt);

    // Thông tin kiện hàng do server tự điền từ Product (bước "get info" trước khi tính phí)
    public sealed class GhnParcelInfoResponse
    {
        public string? ProductName { get; init; }
        public int CommercialQuantity { get; init; }
        public int? ProductWeightGram { get; init; }
        public int? ProductLengthCm { get; init; }
        public int? ProductWidthCm { get; init; }
        public int? ProductHeightCm { get; init; }

        // Estimate only: never overwrites or constrains confirmed physical parcels.
        public long? EstimatedTotalWeightGram { get; init; }
        public bool EstimatedOverLimit { get; init; }
        public GhnContactSnapshotDto? Sender { get; init; }
        public GhnContactSnapshotDto? Receiver { get; init; }
        public Guid NegotiationId { get; init; }
        public int ServiceTypeId { get; init; }

        public GhnLightParcelSnapshotDto? LightParcel { get; init; }

        // Optional prefill for a single product. User must confirm actual packaging; Quantity = 1.
        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();

        public bool HasProductDimensions { get; init; }

        // Requires actual packaging input, not a restriction on using GHN.
        public bool RequiresPackagingDimensions { get; init; }
    }

    // Kết quả tính phí GHN (không kèm breakdown phí)
    public sealed class GhnShippingPreviewResponse
    {
        public GhnShippingInfo? ShippingInfo { get; set; }
        public string? PreviewToken { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public Guid NegotiationId { get; init; }
        public int ServiceTypeId { get; init; }

        public decimal TotalFee { get; init; }

        public DateTimeOffset? ExpectedDeliveryAt { get; init; }

        // Thông số cấp đơn thực tế dùng cho preview, kể cả hàng nặng.
        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
        public int ParcelCount { get; init; }

        // Giá trị thực tế đã dùng để gọi GHN (sau khi merge ghi đè của FE).
        public GhnLightParcelSnapshotDto? LightParcel { get; init; }

        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();
    }
    public sealed record GhnLeadtimeResponse(DateTimeOffset ExpectedDeliveryAt,
        DateTimeOffset? FromEstimateDate, DateTimeOffset? ToEstimateDate);
}
