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
    }
}
