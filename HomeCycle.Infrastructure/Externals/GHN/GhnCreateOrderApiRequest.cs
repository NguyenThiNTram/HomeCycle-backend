using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    internal sealed class GhnCreateOrderApiRequest
    {
            [JsonPropertyName("client_order_code")]
            public required string ClientOrderCode { get; init; }

            [JsonPropertyName("from_name")]
            public required string FromName { get; init; }

            [JsonPropertyName("from_phone")]
            public required string FromPhone { get; init; }

            [JsonPropertyName("from_address")]
            public required string FromAddress { get; init; }

            // GHN cho phép truyền text hỗ trợ, nhưng bắt buộc xử lý Id bên dưới
            [JsonPropertyName("from_ward_name")]
            public required string FromWardName { get; init; }

            [JsonPropertyName("from_district_name")]
            public required string FromDistrictName { get; init; }

            [JsonPropertyName("from_province_name")]
            public required string FromProvinceName { get; init; }

            [JsonPropertyName("to_name")]
            public required string ToName { get; init; }

            [JsonPropertyName("to_phone")]
            public required string ToPhone { get; init; }

            [JsonPropertyName("to_address")]
            public required string ToAddress { get; init; }

            // ====================================================================
            // BỔ SUNG BẮT BUỘC: GHN cần Mã ID để định tuyến và tính toán biểu phí ship
            // ====================================================================
            [JsonPropertyName("to_district_id")]
            public required int ToDistrictId { get; init; }

            [JsonPropertyName("to_ward_code")]
            public required string ToWardCode { get; init; }

            [JsonPropertyName("service_type_id")]
            public int ServiceTypeId { get; init; }

            [JsonPropertyName("payment_type_id")]
            public int PaymentTypeId { get; init; }

            [JsonPropertyName("cod_amount")]
            public int CodAmount { get; init; }

            [JsonPropertyName("insurance_value")]
            public int InsuranceValue { get; init; }

            [JsonPropertyName("required_note")]
            public required string RequiredNote { get; init; }

            [JsonPropertyName("note")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Note { get; init; }

            [JsonPropertyName("content")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Content { get; init; }

            // Đổi sang int bắt buộc, không được để null hoặc ẩn đi
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

            // Xóa bỏ nullable và JsonIgnore, đây là trường bắt buộc phải có mảng cụ thể
            [JsonPropertyName("items")]
            public required IReadOnlyList<GhnCreateOrderApiItem> Items { get; init; } = Array.Empty<GhnCreateOrderApiItem>();
        }

    internal sealed class GhnCreateOrderApiItem
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

    internal sealed class GhnCreateOrderData
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; init; } = string.Empty;

        [JsonPropertyName("total_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal TotalFee { get; init; }

        [JsonPropertyName("expected_delivery_time")]
        public DateTimeOffset? ExpectedDeliveryTime { get; init; }

        [JsonPropertyName("fee")]
        public GhnCreateOrderFeeData? Fee { get; init; }
    }

    internal sealed class GhnCreateOrderFeeData
    {
        [JsonPropertyName("main_service")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal MainService { get; init; }

        [JsonPropertyName("cod_fee")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal CodFee { get; init; }
    }
}
