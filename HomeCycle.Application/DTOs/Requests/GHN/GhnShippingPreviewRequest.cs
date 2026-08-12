using HomeCycle.Application.DTOs.Responses.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class GhnShippingPreviewRequest
    {
        public GhnContactSnapshotDto? Sender { get; init; }
        public GhnContactSnapshotDto? Receiver { get; init; }

        // 2 = Hàng nhẹ, 5 = Hàng nặng (mặc định Hàng nhẹ)
        public int ServiceTypeId { get; init; } = 2;

        public string? RequiredNote { get; init; }

        // Ghi đè tùy chọn: nếu null, server tự lấy từ Product.
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // Bắt buộc khi ServiceTypeId = 5. Nếu rỗng, server tự dựng 1 kiện từ Product.
        public IReadOnlyList<CalculateGhnFeeItemRequest> Items { get; init; }
            = Array.Empty<CalculateGhnFeeItemRequest>();
    }
}
