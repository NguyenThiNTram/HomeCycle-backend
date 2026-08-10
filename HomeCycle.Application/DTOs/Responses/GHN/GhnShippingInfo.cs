using HomeCycle.Application.DTOs.Requests.Agreements;
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

        //  1 - Express, 2 - Standard
        public int? ServiceTypeId { get; init; }

        // 1 - Shop/Seller. 2 - Buyer/Consignee
        public int? PaymentTypeId { get; init; }

        public string? RequiredNote { get; init; } //CHOTHUHANG, CHOXEMHANGKHONGTHU, KHONGCHOXEMHANG 

        // Thông số kiện đã dùng để tính phí/preview
        public IReadOnlyList<GhnItemSnapshotDto> Items { get; init; }
            = Array.Empty<GhnItemSnapshotDto>();

        // Chỉ có sau khi Preview thành công
        public GhnQuoteSnapshotDto? Quote { get; init; }
    }
}
