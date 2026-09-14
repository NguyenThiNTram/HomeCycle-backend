using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    internal sealed class GhnApiItemRequest
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("weight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int WeightGram { get; init; }

        [JsonPropertyName("length")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int LengthCm { get; init; }

        [JsonPropertyName("width")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int WidthCm { get; init; }

        [JsonPropertyName("height")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int HeightCm { get; init; }
    }
}
