using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class GhnFeeQuoteRequest
    {
        public int FromDistrictId { get; init; }
        public required string FromWardCode { get; init; }

        public int ToDistrictId { get; init; }
        public required string ToWardCode { get; init; }

        public int ServiceTypeId { get; init; }

        public int WeightGram { get; init; }
        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        public IReadOnlyList<GhnFeeQuoteItem> Items { get; init; }
            = Array.Empty<GhnFeeQuoteItem>();
    }

    public sealed class GhnFeeQuoteItem
    {
        public required string Name { get; init; }
        public int Quantity { get; init; }

        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }
}
