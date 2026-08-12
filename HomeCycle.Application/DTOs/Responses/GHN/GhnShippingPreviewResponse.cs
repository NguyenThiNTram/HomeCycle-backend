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
        public Guid NegotiationId { get; init; }

        // 2 = Hàng nhẹ, 5 = Hàng nặng 
        //public int ServiceTypeId { get; init; } = 2; //mặc định Hàng nhẹ
        public int ServiceTypeId { get; init; } 

        public GhnLightParcelSnapshotDto? LightParcel { get; init; }

        // Hàng nặng: server tự dựng 1 kiện từ Product (số lượng theo Offer).
        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();

        public bool HasProductDimensions { get; init; }
    }

    // Kết quả tính phí GHN (không kèm breakdown phí)
    public sealed class GhnShippingPreviewResponse
    {
        public Guid NegotiationId { get; init; }
        public int ServiceTypeId { get; init; }

        public decimal TotalFee { get; init; }

        public DateTimeOffset? ExpectedDeliveryAt { get; init; }

        // Giá trị thực tế đã dùng để gọi GHN (sau khi merge ghi đè của FE).
        public GhnLightParcelSnapshotDto? LightParcel { get; init; }

        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();
    }
}
