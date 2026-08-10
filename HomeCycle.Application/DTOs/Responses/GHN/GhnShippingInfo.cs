using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public class GhnShippingInfo
    {
        public GhnContactSnapshotDto? Sender { get; init; }
        public GhnContactSnapshotDto? Receiver { get; init; }   // Address/District/Ward có thể null nếu Buyer chưa điền

        // 2 = Hàng nhẹ, 5 = Hàng nặng.
        public int? ServiceTypeId { get; init; }

        // MVP cố định buyer/consignee trả phí.
        // Không nhận trường này từ FE.
        public int PaymentTypeId { get; init; } = 2;

        public string? RequiredNote { get; init; } //CHOTHUHANG, CHOXEMHANGKHONGTHU, KHONGCHOXEMHANG 

        // Chỉ sử dụng khi ServiceTypeId = 2.
        public GhnLightParcelSnapshotDto? LightParcel { get; init; }

        // Chỉ sử dụng khi ServiceTypeId = 5.
        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();

        public GhnQuoteStatus QuoteStatus { get; init; } = GhnQuoteStatus.NotCalculated;

        // Chỉ tồn tại sau khi tính phí thành công.
        public GhnQuoteSnapshotDto? Quote { get; init; }
    }
}
