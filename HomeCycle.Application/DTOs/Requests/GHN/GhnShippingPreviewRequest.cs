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

        // 2 = một kiện dưới 20kg; 5 = từ 20kg hoặc nhiều kiện.
        public int ServiceTypeId { get; init; } = 2;

        public string? RequiredNote { get; init; }

        public string? Content { get; init; }

        // Số kiện đóng gói thực tế, không phải số lượng sản phẩm.
        public int ParcelCount { get; init; } = 1;

        // Thông số cấp đơn sau đóng gói. Null: suy từ Product khi có đủ dữ liệu;
        // nhiều sản phẩm/kiện cần cung cấp kích thước đóng gói thực tế.
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // Type 5 cần thông số từng item. Nếu rỗng, dựng danh sách từ Product và số lượng mua.
        public IReadOnlyList<CalculateGhnFeeItemRequest> Items { get; init; }
            = Array.Empty<CalculateGhnFeeItemRequest>();
    }
}
