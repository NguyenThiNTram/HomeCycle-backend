using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class GhnWebhookRequest
    {
        [JsonPropertyName("ShopID")]
        public int ShopId { get; init; }

        [JsonPropertyName("OrderCode")]
        public string? OrderCode { get; init; }

        [JsonPropertyName("ClientOrderCode")]
        public string? ClientOrderCode { get; init; }

        [JsonPropertyName("Status")]
        public string? Status { get; init; }

        [JsonPropertyName("Time")]
        public DateTimeOffset? Time { get; init; }

        [JsonPropertyName("Type")]
        public string? Type { get; init; }
    }
}
