using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public class GhnCreateOrderRequest
    {
        public required string ClientOrderCode { get; init; }

        public required string FromName { get; init; }
        public required string FromPhone { get; init; }
        public required string FromAddress { get; init; }
        public required string FromWardName { get; init; }
        public required string FromDistrictName { get; init; }
        public required string FromProvinceName { get; init; }
        public int FromDistrictId { get; init; }
        public string? FromWardCode { get; init; }

        public required string ToName { get; init; }
        public required string ToPhone { get; init; }
        public required string ToAddress { get; init; }
        public required int ToDistrictId { get; init; }
        public required string ToWardCode { get; init; }
        public string ToWardName { get; init; } = string.Empty;
        public string ToDistrictName { get; init; } = string.Empty;
        public string ToProvinceName { get; init; } = string.Empty;
        public int ParcelCount { get; init; } = 1;

        public int ServiceTypeId { get; init; }

        // HomeCycle đã thu phí ship từ buyer.
        // GHN trừ phí qua tài khoản ShopId.
        public int PaymentTypeId { get; init; } = 1;

        public int CodAmount { get; init; } = 0;
        public int InsuranceValue { get; init; } = 0;

        public required string RequiredNote { get; init; }
        public string? Note { get; init; }
        public string? Content { get; init; }

        // Thông số đóng gói cấp đơn, bắt buộc cho cả type 2 và 5.
        public int? WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // Bắt buộc cho service type 5.
        public IReadOnlyList<GhnCreateOrderItemRequest> Items { get; init; }
            = Array.Empty<GhnCreateOrderItemRequest>();
    }

    public sealed class GhnCreateOrderItemRequest
    {
        public required string Name { get; init; }
        public string? Code { get; init; }
        public int Quantity { get; init; }
        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }
}
