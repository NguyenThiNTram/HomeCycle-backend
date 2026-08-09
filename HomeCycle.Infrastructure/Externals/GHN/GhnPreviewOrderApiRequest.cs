using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    internal sealed class GhnPreviewOrderApiRequest
    {
        // HomeCycle bắt buộc gửi để không fallback về ShopId.
        [JsonPropertyName("from_name")]
        public required string FromName { get; init; }

        [JsonPropertyName("from_phone")]
        public required string FromPhone { get; init; }

        [JsonPropertyName("from_address")]
        public required string FromAddress { get; init; }

        [JsonPropertyName("from_ward_name")]
        public required string FromWardName { get; init; }

        [JsonPropertyName("from_district_name")]
        public required string FromDistrictName { get; init; }

        [JsonPropertyName("to_name")]
        public required string ToName { get; init; }

        [JsonPropertyName("to_phone")]
        public required string ToPhone { get; init; }

        [JsonPropertyName("to_address")]
        public required string ToAddress { get; init; }

        [JsonPropertyName("to_ward_name")]
        public required string ToWardName { get; init; }

        [JsonPropertyName("to_district_name")]
        public required string ToDistrictName { get; init; }

        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; init; }

        [JsonPropertyName("payment_type_id")]
        public int PaymentTypeId { get; init; }

        [JsonPropertyName("required_note")]
        public required string RequiredNote { get; init; }

        [JsonPropertyName("weight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? WeightGram { get; init; }

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

    //Preview response
    internal sealed class GhnPreviewOrderData
    {
        [JsonPropertyName("total_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal TotalFee { get; init; }

        [JsonPropertyName("expected_delivery_time")]
        public DateTimeOffset ExpectedDeliveryTime { get; init; }
    }
}
