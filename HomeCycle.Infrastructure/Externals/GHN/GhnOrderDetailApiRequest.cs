using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    //internal sealed class GhnOrderDetailApiRequest
    //{
    //    [JsonPropertyName("order_code")]
    //    public required string OrderCode { get; init; }
    //}

    internal sealed record GhnOrderDetailApiRequest(
        [property: JsonPropertyName("order_code")]
        string OrderCode);

    internal sealed class GhnOrderDetailData
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; init; } = string.Empty;

        [JsonPropertyName("client_order_code")]
        public string? ClientOrderCode { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("service_type_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? ServiceTypeId { get; init; }

        [JsonPropertyName("weight")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? WeightGram { get; init; }

        [JsonPropertyName("converted_weight")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? ConvertedWeightGram { get; init; }

        [JsonPropertyName("length")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? LengthCm { get; init; }

        [JsonPropertyName("width")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? WidthCm { get; init; }

        [JsonPropertyName("height")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? HeightCm { get; init; }

        [JsonPropertyName("required_note")]
        public string? RequiredNote { get; init; }

        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("note")]
        public string? Note { get; init; }

        [JsonPropertyName("leadtime")]
        public DateTimeOffset? Leadtime { get; init; }

        [JsonPropertyName("finish_date")]
        public DateTimeOffset? FinishDate { get; init; }

        [JsonPropertyName("order_date")]
        public string? OrderDate { get; init; }

        [JsonPropertyName("updated_date")]
        public string? UpdatedDate { get; init; }

        [JsonPropertyName("to_name")]
        public string? ToName { get; init; }

        [JsonPropertyName("to_address")]
        public string? ToAddress { get; init; }

        [JsonPropertyName("log")]
        public IReadOnlyList<GhnOrderDetailLogData>? Log { get; init; }
    }

    internal sealed class GhnOrderDetailLogData
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("updated_date")]
        public DateTimeOffset UpdatedDate { get; init; }
    }
}
