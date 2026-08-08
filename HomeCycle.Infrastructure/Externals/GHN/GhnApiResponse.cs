using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    internal sealed class GhnApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; init; }

        [JsonPropertyName("code_message")]
        public string? CodeMessage { get; init; }
    }

    internal sealed class GhnProvinceData
    {
        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; init; }

        [JsonPropertyName("ProvinceName")]
        public string ProvinceName { get; init; } = string.Empty;

        [JsonPropertyName("Code")]
        public string? Code { get; init; }

        [JsonPropertyName("Status")]
        public int Status { get; init; }
    }

    internal sealed class GhnDistrictData
    {
        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; init; }

        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; init; }

        [JsonPropertyName("DistrictName")]
        public string DistrictName { get; init; } = string.Empty;

        [JsonPropertyName("Code")]
        public string? Code { get; init; }

        [JsonPropertyName("Type")]
        public int Type { get; init; }

        [JsonPropertyName("SupportType")]
        public int SupportType { get; init; }

        [JsonPropertyName("Status")]
        public int Status { get; init; }
    }

    internal sealed class GhnWardData
    {
        [JsonPropertyName("WardCode")]
        public string WardCode { get; init; } = string.Empty;

        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; init; }

        [JsonPropertyName("WardName")]
        public string WardName { get; init; } = string.Empty;

        [JsonPropertyName("SupportType")]
        public int SupportType { get; init; }

        [JsonPropertyName("Status")]
        public int Status { get; init; }
    }

    internal sealed record GhnDistrictRequest(
        [property: JsonPropertyName("province_id")] int ProvinceId);

    internal sealed record GhnWardRequest(
        [property: JsonPropertyName("district_id")] int DistrictId);
}
