using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public sealed class GhnService : IGhnService
    {
        private const string ProvinceCacheKey = "ghn:locations:provinces";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly GhnSettings _settings;

        public GhnService(HttpClient httpClient, IMemoryCache cache, IOptions<GhnSettings> settings)
        {
            _httpClient = httpClient;
            _cache = cache;
            _settings = settings.Value;
        }

        public async Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _cache.GetOrCreateAsync(
                ProvinceCacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(_settings.AddressCacheHours);

                    var data = await SendAsync<GhnProvinceData>(
                        HttpMethod.Get,
                        "master-data/province",
                        body: null,
                        cancellationToken);

                    return (IReadOnlyList<GhnProvinceResponse>)data
                        .Select(x => new GhnProvinceResponse(
                            x.ProvinceId,
                            x.ProvinceName,
                            x.Code,
                            x.Status))
                        .OrderBy(x => x.ProvinceName)
                        .ToList();
                });

            return result ?? Array.Empty<GhnProvinceResponse>();
        }

        public async Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(
            int provinceId,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(provinceId);

            var cacheKey = $"ghn:locations:province:{provinceId}:districts";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(_settings.AddressCacheHours);

                    var data = await SendAsync<GhnDistrictData>(
                        HttpMethod.Post,
                        "master-data/district",
                        new GhnDistrictRequest(provinceId),
                        cancellationToken);

                    return (IReadOnlyList<GhnDistrictResponse>)data
                        .Select(x => new GhnDistrictResponse(
                            x.DistrictId,
                            x.ProvinceId,
                            x.DistrictName,
                            x.Code,
                            x.Type,
                            x.SupportType,
                            x.Status))
                        .OrderBy(x => x.DistrictName)
                        .ToList();
                });

            return result ?? Array.Empty<GhnDistrictResponse>();
        }

        public async Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(
            int districtId,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(districtId);

            var cacheKey = $"ghn:locations:district:{districtId}:wards";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(_settings.AddressCacheHours);

                    var data = await SendAsync<GhnWardData>(
                        HttpMethod.Post,
                        "master-data/ward",
                        new GhnWardRequest(districtId),
                        cancellationToken);

                    return (IReadOnlyList<GhnWardResponse>)data
                        .Select(x => new GhnWardResponse(
                            x.WardCode,
                            x.DistrictId,
                            x.WardName,
                            x.SupportType,
                            x.Status))
                        .OrderBy(x => x.WardName)
                        .ToList();
                });

            return result ?? Array.Empty<GhnWardResponse>();
        }

        private async Task<IReadOnlyList<TData>> SendAsync<TData>(
            HttpMethod method,
            string relativeUrl,
            object? body,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, relativeUrl);

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            GhnApiResponse<List<TData>>? payload;

            try
            {
                payload = JsonSerializer.Deserialize<GhnApiResponse<List<TData>>>(
                    json,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new GhnApiException(
                    statusCode: response.StatusCode,
                    message: "GHN trả về dữ liệu không đúng định dạng JSON.",
                    codeMessage: "JSON_PARSING_ERROR",
                    innerException: exception);
            }

            if (!response.IsSuccessStatusCode || payload is null || payload.Code != 200)
            {
                var statusCode = payload is null ? response.StatusCode : (HttpStatusCode)payload.Code;
                var errorMessage = payload?.Message ?? "Không thể kết nối hoặc không có phản hồi từ dịch vụ GHN.";

                throw new GhnApiException(
                    statusCode: statusCode,
                    message: $"Lỗi hệ thống GHN: {errorMessage}",
                    codeMessage: payload?.CodeMessage ?? "GHN_SERVICE_ERROR");
            }

            if (payload.Data is null)
            {
                throw new GhnApiException(
                    statusCode: (HttpStatusCode)payload.Code,
                    message: payload.Message ?? "GHN trả về trạng thái thành công nhưng danh sách dữ liệu bị rỗng (null).",
                    codeMessage: payload.CodeMessage ?? "EMPTY_DATA_ERROR");
            }

            return payload.Data;
        }

        //Hàm gửi request dùng riêng cho các API GHN trả về kết quả dạng Object đơn lẻ (không phải List)
        private async Task<TData> SendSingleAsync<TData>(
            HttpMethod method,
            string relativeUrl,
            object? body,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, relativeUrl);

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Sửa đổi cốt lõi: Nhận TData trực tiếp thay vì List<TData>
            GhnApiResponse<TData>? payload;

            try
            {
                payload = JsonSerializer.Deserialize<GhnApiResponse<TData>>(
                    json,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new GhnApiException(
                    statusCode: response.StatusCode,
                    message: "GHN trả về dữ liệu không đúng định dạng JSON.",
                    codeMessage: "JSON_PARSING_ERROR",
                    innerException: exception);
            }

            if (!response.IsSuccessStatusCode || payload is null || payload.Code != 200)
            {
                var statusCode = payload is null ? response.StatusCode : (HttpStatusCode)payload.Code;
                var errorMessage = payload?.Message ?? "Không thể kết nối hoặc không có phản hồi từ dịch vụ GHN.";

                throw new GhnApiException(
                    statusCode: statusCode,
                    message: $"Lỗi hệ thống GHN: {errorMessage}",
                    codeMessage: payload?.CodeMessage ?? "GHN_SERVICE_ERROR");
            }

            if (payload.Data is null)
            {
                throw new GhnApiException(
                    statusCode: (HttpStatusCode)payload.Code,
                    message: payload.Message ?? "GHN trả về trạng thái thành công nhưng dữ liệu bị rỗng (null).",
                    codeMessage: payload.CodeMessage ?? "EMPTY_DATA_ERROR");
            }

            return payload.Data;
        }

        public async Task<GhnFeeQuoteResponse> GetShippingFeeAsync(GhnFeeQuoteRequest request, CancellationToken cancellationToken = default)
        {
            var apiRequest = MapFeeRequest(request);

            // Sử dụng hàm SendSingleAsync mới để xử lý JSON Object đơn lẻ thay vì List
            // endpoint của GHN tính phí ship: "shipping-order/fee" (hoặc truyền endpoint từ _settings tùy cấu hình)
            var response = await SendSingleAsync<GhnCalculateFeeData>(
                HttpMethod.Post,
                "v2/shipping-order/fee",
                apiRequest,
                cancellationToken);

            // Khớp với Record / Class GhnFeeQuoteResponse ở tầng Application của bạn
            return new GhnFeeQuoteResponse(
                ServiceFee: response.ServiceFee,
                TotalFee: response.Total);
        }

        private static GhnCalculateFeeApiRequest MapFeeRequest(GhnFeeQuoteRequest request)
        {
            // nhận diện gói dịch vụ
            bool isLightGoods = request.ServiceTypeId == 2;
            bool isHeavyGoods = request.ServiceTypeId == 5;

            return new GhnCalculateFeeApiRequest
            {
                FromDistrictId = request.FromDistrictId,
                FromWardCode = request.FromWardCode,
                ToDistrictId = request.ToDistrictId,
                ToWardCode = request.ToWardCode,
                ServiceTypeId = request.ServiceTypeId,
                WeightGram = request.WeightGram,

                // Chuẩn hóa kích thước: Nếu client truyền thiếu/bằng 0, tự động lấy 10cm để tránh lỗi API GHN
                LengthCm = request.LengthCm > 0 ? request.LengthCm : 10,
                WidthCm = request.WidthCm > 0 ? request.WidthCm : 10,
                HeightCm = request.HeightCm > 0 ? request.HeightCm : 10
            };
        }
    }
}
