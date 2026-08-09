using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class CalculateGhnFeeRequest
    {
        // 2 = Hàng nhẹ, 5 = Hàng nặng
        public int ServiceTypeId { get; init; }

        // GHN yêu cầu weight cấp đơn
        public int WeightGram { get; init; }

        // Bắt buộc khi ServiceTypeId = 2
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // Bắt buộc khi ServiceTypeId = 5
        public IReadOnlyList<CalculateGhnFeeItemRequest> Items { get; init; }
            = Array.Empty<CalculateGhnFeeItemRequest>();
    }

    public sealed class CalculateGhnFeeItemRequest
    {
        public int Quantity { get; init; }

        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }
}
