using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Agreements
{
    public sealed class ShippingFeePreviewRequest
    {
        public int FromDistrictId { get; init; }
        public required string FromWardCode { get; init; }

        public int ToDistrictId { get; init; }
        public required string ToWardCode { get; init; }

        // 2 = Hàng nhẹ, 5 = Hàng nặng (mặc định Hàng nhẹ)
        public int ServiceTypeId { get; init; } = 2;

        // Tùy chọn: nếu không truyền, service tự lấy thông số từ Product (logic hiện tại).
        // FE có thể truyền giá trị để ghi đè (chỉnh sửa kích cỡ/khối lượng).
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // Bắt buộc khi ServiceTypeId = 5 (Hàng nặng): mỗi item là 1 kiện hàng.
        // Nếu rỗng, service tự dựng 1 item từ thông số Product.
        public IReadOnlyList<ShippingFeePreviewItemRequest> Items { get; init; }
            = Array.Empty<ShippingFeePreviewItemRequest>();

        // Tiền thu hộ cho người gửi (COD)
        public int? CodValue { get; init; }
    }

    public sealed class ShippingFeePreviewItemRequest
    {
        public required string Name { get; init; }
        public int Quantity { get; init; }

        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
        public int WeightGram { get; init; }
    }
}
