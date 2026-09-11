using System.Text.Json.Serialization;

namespace HomeCycle.Infrastructure.Externals.GHN;

internal sealed record GhnAvailableServicesApiRequest(
    [property: JsonPropertyName("shop_id")] int ShopId,
    [property: JsonPropertyName("from_district")] int FromDistrictId,
    [property: JsonPropertyName("to_district")] int ToDistrictId);

internal sealed class GhnAvailableServiceData
{
    [JsonPropertyName("service_id")]
    public int ServiceId { get; init; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("service_type_id")]
    public int ServiceTypeId { get; init; }
}
