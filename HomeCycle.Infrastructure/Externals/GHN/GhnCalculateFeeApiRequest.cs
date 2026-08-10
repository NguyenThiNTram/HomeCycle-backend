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
        [JsonPropertyName("from_district_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FromDistrictId { get; init; }

        [JsonPropertyName("from_ward_code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FromWardCode { get; init; }

        [JsonPropertyName("to_district_id")]
        public int ToDistrictId { get; init; }

        [JsonPropertyName("to_ward_code")]
        public required string ToWardCode { get; init; }

        [JsonPropertyName("service_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ServiceId { get; init; }

        [JsonPropertyName("service_type_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ServiceTypeId { get; init; }

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

        [JsonPropertyName("insurance_value")]
        public int InsuranceValue { get; init; }

        [JsonPropertyName("cod_value")]
        public int CodValue { get; init; }

        [JsonPropertyName("cod_failed_amount")]
        public int CodFailedAmount { get; init; }

        [JsonPropertyName("coupon")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Coupon { get; init; }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<GhnCalculateFeeItemApiRequest>? Items { get; init; }
    }

    internal sealed class GhnCalculateFeeItemApiRequest
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }

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

    internal sealed class GhnCalculateFeeData
    {
        [JsonPropertyName("total")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Total { get; init; }

        [JsonPropertyName("service_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal ServiceFee { get; init; }

        [JsonPropertyName("insurance_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal InsuranceFee { get; init; }

        [JsonPropertyName("pick_station_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal PickStationFee { get; init; }

        [JsonPropertyName("coupon_value")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal CouponValue { get; init; }

        [JsonPropertyName("r2s_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal R2sFee { get; init; }

        [JsonPropertyName("document_return")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal DocumentReturnFee { get; init; }

        [JsonPropertyName("double_check")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal DoubleCheckFee { get; init; }

        [JsonPropertyName("cod_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal CodFee { get; init; }

        [JsonPropertyName("pick_remote_areas_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal PickRemoteAreasFee { get; init; }

        [JsonPropertyName("deliver_remote_areas_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal DeliverRemoteAreasFee { get; init; }

        [JsonPropertyName("cod_failed_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal CodFailedFee { get; init; }

        [JsonPropertyName("fee")]
        public GhnCalculateFeeBreakdownData? FeeBreakdown { get; init; }
    }

    internal sealed class GhnCalculateFeeBreakdownData
    {
        [JsonPropertyName("coupon")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Coupon { get; init; }

        [JsonPropertyName("insurance")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Insurance { get; init; }

        [JsonPropertyName("main_service")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal MainService { get; init; }

        [JsonPropertyName("r2s")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal R2s { get; init; }

        [JsonPropertyName("return")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal ReturnFee { get; init; }

        [JsonPropertyName("station_do")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal StationDo { get; init; }

        [JsonPropertyName("station_pu")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal StationPu { get; init; }
    }
}
