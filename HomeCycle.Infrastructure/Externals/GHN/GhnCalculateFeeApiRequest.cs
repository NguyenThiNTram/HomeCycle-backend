using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    internal sealed class GhnCalculateFeeApiRequest
    {
        // GHN không bắt buộc, nhưng HomeCycle phải gửi vì ShopId
        // không đại diện cho địa chỉ seller thật.
        [JsonPropertyName("from_district_id")]
        public int FromDistrictId { get; init; }

        [JsonPropertyName("from_ward_code")]
        public required string FromWardCode { get; init; }

        [JsonPropertyName("to_district_id")]
        public int ToDistrictId { get; init; }

        [JsonPropertyName("to_ward_code")]
        public required string ToWardCode { get; init; }

        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; init; }

        [JsonPropertyName("weight")]
        public int WeightGram { get; init; }

        [JsonPropertyName("length")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? LengthCm { get; init; }

        [JsonPropertyName("width")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? WidthCm { get; init; }

        [JsonPropertyName("height")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? HeightCm { get; init; }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<GhnApiItemRequest>? Items { get; init; }
    }

    //Calculate Fee response
    internal sealed class GhnCalculateFeeData
    {
        [JsonPropertyName("total")]
        public decimal Total { get; init; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; init; }
    }

    internal sealed class GhnCalculateFeeItemApiRequest
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("weight")]
        public int WeightGram { get; init; }

        [JsonPropertyName("length")]
        public int LengthCm { get; init; }

        [JsonPropertyName("width")]
        public int WidthCm { get; init; }

        [JsonPropertyName("height")]
        public int HeightCm { get; init; }
    }
}
