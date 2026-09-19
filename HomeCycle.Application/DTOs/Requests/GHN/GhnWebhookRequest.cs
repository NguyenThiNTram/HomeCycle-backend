using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        [JsonPropertyName("Description")]
        public string? Description { get; init; }

        [JsonPropertyName("Reason")]
        public string? Reason { get; init; }

        [JsonPropertyName("ReasonCode")]
        public string? ReasonCode { get; init; }

        [JsonPropertyName("CODAmount")]
        public int CodAmount { get; init; }

        [JsonPropertyName("CODTransferDate")]
        public DateTimeOffset? CodTransferDate { get; init; }

        [JsonPropertyName("Weight")]
        public int Weight { get; init; }

        [JsonPropertyName("ConvertedWeight")]
        public int ConvertedWeight { get; init; }

        [JsonPropertyName("Length")]
        public int Length { get; init; }

        [JsonPropertyName("Width")]
        public int Width { get; init; }

        [JsonPropertyName("Height")]
        public int Height { get; init; }

        [JsonPropertyName("PaymentType")]
        public int PaymentType { get; init; }

        [JsonPropertyName("IsPartialReturn")]
        public bool IsPartialReturn { get; init; }

        [JsonPropertyName("PartialReturnCode")]
        public string? PartialReturnCode { get; init; }

        [JsonPropertyName("Fee")]
        public JsonElement? Fee { get; init; }

        [JsonPropertyName("TotalFee")]
        public int TotalFee { get; init; }

        [JsonPropertyName("Warehouse")]
        public string? Warehouse { get; init; }

        [JsonPropertyName("ShipperName")]
        public string? ShipperName { get; init; }

        [JsonPropertyName("ShipperPhone")]
        public string? ShipperPhone { get; init; }

        [JsonPropertyName("PodURL")]
        public string? PodUrl { get; init; }
    }
}
