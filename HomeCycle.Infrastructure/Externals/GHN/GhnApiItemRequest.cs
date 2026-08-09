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
