using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class CalculateGhnFeeRequest
    {
        public int FromDistrictId { get; init; }
        public required string FromWardCode { get; init; }

        public int ToDistrictId { get; init; }
        public required string ToWardCode { get; init; }
        
        public int ServiceTypeId { get; init; } // 2 = Hàng nhẹ, 5 = Hàng nặng
        public int WeightGram { get; init; }

        // ServiceTypeId = 2
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        // ServiceTypeId = 5
        public IReadOnlyList<CalculateGhnFeeItemRequest> Items { get; init; }
            = Array.Empty<CalculateGhnFeeItemRequest>();
    }

    public sealed class CalculateGhnFeeItemRequest
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
