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
        public HomeCycle.Domain.Enums.AgreementType? AgreementType { get; init; }
        public HomeCycle.Domain.Enums.DeliveryMethod? DeliveryMethod { get; init; }
        public GhnContactSnapshotDto? Sender { get; init; }
        public GhnContactSnapshotDto? Receiver { get; init; }

        // 2 = một kiện dưới 20kg; 5 = từ 20kg hoặc nhiều kiện
        public int ServiceTypeId { get; init; } = 2;

        public string? RequiredNote { get; init; }

        public string? Content { get; init; }

        // Số kiện đóng gói thực tế, không phải số lượng sản phẩm
        public int ParcelCount { get; init; } = 1;

        // WeightGram phải bằng tổng cân Items. Không suy từ Product/Agreement quantity.
        // Một kiện: kích thước root khớp kiện; nhiều kiện chờ xác minh GHN Staging.
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // HomeCycle: mỗi item là một kiện vật lý, Quantity = 1; bắt buộc cả type 2 và 5.
        public IReadOnlyList<CalculateGhnFeeItemRequest> Items { get; init; }
            = Array.Empty<CalculateGhnFeeItemRequest>();
    }
    public sealed class GhnLeadtimeRequest
    {
        public HomeCycle.Domain.Enums.AgreementType? AgreementType { get; init; }
        public HomeCycle.Domain.Enums.DeliveryMethod? DeliveryMethod { get; init; }
        public int? FromDistrictId { get; init; }
        public string? FromWardCode { get; init; }
        public int ToDistrictId { get; init; }
        public string ToWardCode { get; init; } = string.Empty;
        public int? ServiceTypeId { get; init; }
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }
    }
}
