using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public async Task<GhnLeadtimeResponse> GetLeadtimeAsync(GhnLeadtimeRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(request.ToWardCode) ||
                request.FromDistrictId is <= 0 || request.FromWardCode != null && string.IsNullOrWhiteSpace(request.FromWardCode) ||
                request.ServiceTypeId is not (null or 2 or 5) || request.WeightGram is <= 0 ||
                request.LengthCm is <= 0 || request.WidthCm is <= 0 || request.HeightCm is <= 0)
                throw new ArgumentException("Thông tin tuyến/thông số leadtime không hợp lệ.");
            if (request.ServiceTypeId == 2 && request.WeightGram >= 20_000)
                throw new ArgumentException("Hàng từ 20kg phải dùng type 5.");
            var body = new Dictionary<string, object> { ["to_district_id"] = request.ToDistrictId, ["to_ward_code"] = request.ToWardCode.Trim() };
            void Add(string key, object? value) { if (value != null) body[key] = value; }
            Add("from_district_id", request.FromDistrictId); Add("from_ward_code", request.FromWardCode?.Trim());
            Add("service_type_id", request.ServiceTypeId); Add("weight", request.WeightGram);
            Add("length", request.LengthCm); Add("width", request.WidthCm); Add("height", request.HeightCm);
            var data = await SendSingleAsync<GhnLeadtimeData>(HttpMethod.Post, "v2/shipping-order/leadtime", body, cancellationToken);
            if (data.Leadtime is null or <= 0)
                throw new GhnApiException(HttpStatusCode.BadGateway, "GHN không trả thời gian dự kiến hợp lệ.", "INVALID_LEADTIME");
            try
            {
                return new GhnLeadtimeResponse(DateTimeOffset.FromUnixTimeSeconds(data.Leadtime.Value), data.Range?.From, data.Range?.To);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new GhnApiException(HttpStatusCode.BadGateway, "GHN trả timestamp ngoài phạm vi.", "INVALID_LEADTIME", ex);
            }
        }

        public async Task<IReadOnlyList<GhnCancelOrderResponse>> CancelOrdersAsync(IReadOnlyList<string> orderCodes,
            string? reasonCode = null, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (orderCodes == null || orderCodes.Count == 0 || orderCodes.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Cần danh sách mã vận đơn hợp lệ.");
            if (reasonCode != null && !new[] { "GHN-CO001", "GHN-CO002", "GHN-CO003", "GHN-CANCEL-OTHER" }.Contains(reasonCode))
                throw new ArgumentException("Mã lý do hủy GHN không hợp lệ.");
            var codes = orderCodes.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var body = new Dictionary<string, object> { ["order_codes"] = codes };
            if (reasonCode != null) body["reason_code"] = reasonCode;
            if (!string.IsNullOrWhiteSpace(reason)) body["reason"] = reason.Trim();
            var data = await SendAsync<GhnCancelOrderData>(HttpMethod.Post, "v2/switch-status/cancel", body, cancellationToken);
            if (data.Count != codes.Length || codes.Any(code => data.Count(x => string.Equals(code, x.OrderCode, StringComparison.OrdinalIgnoreCase)) != 1))
                throw new GhnApiException(HttpStatusCode.BadGateway, "GHN trả kết quả hủy không đầy đủ/không khớp mã đơn.", "INCOMPLETE_CANCEL_RESULT");
            return data.Select(x => new GhnCancelOrderResponse(x.OrderCode, x.Result, x.Message)).ToArray();
        }
        public async Task<IReadOnlyList<GhnAvailableServiceResponse>> GetAvailableServicesAsync(
            int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default)
        {
            if (fromDistrictId <= 0 || toDistrictId <= 0)
                throw new ArgumentException("Mã quận/huyện của tuyến không hợp lệ.");
            if (_settings.ShopId <= 0)
                throw new GhnApiException(HttpStatusCode.BadGateway, "Shop GHN chưa được cấu hình.", "SHOP_NOT_FOUND");

            var data = await SendAsync<GhnAvailableServiceData>(HttpMethod.Post,
                "v2/shipping-order/available-services",
                new GhnAvailableServicesApiRequest(_settings.ShopId, fromDistrictId, toDistrictId),
                cancellationToken, availableServices: true);
            return data.Select(x => new GhnAvailableServiceResponse(x.ServiceId, x.ShortName, x.ServiceTypeId)).ToArray();
        }

        private async Task<IReadOnlyList<TData>> SendAsync<TData>(
            HttpMethod method,
            string relativeUrl,
            object? body,
            CancellationToken cancellationToken,
            bool availableServices = false)
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

            if (availableServices && response.StatusCode == HttpStatusCode.OK && string.IsNullOrWhiteSpace(json))
                throw new GhnApiException(HttpStatusCode.BadGateway, "Không tìm thấy shop GHN.", "SHOP_NOT_FOUND");

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
                    statusCode: response.IsSuccessStatusCode ? HttpStatusCode.BadGateway : response.StatusCode,
                    message: "GHN trả về dữ liệu không đúng định dạng JSON.",
                    codeMessage: "JSON_PARSING_ERROR",
                    innerException: exception);
            }

            if (!response.IsSuccessStatusCode || payload is null || payload.Code != 200)
            {
                var statusCode = !response.IsSuccessStatusCode ? response.StatusCode :
                    payload?.Code is >= 400 and <= 599 ? (HttpStatusCode)payload.Code : HttpStatusCode.BadGateway;
                if (availableServices && ((int)statusCode < 400 || (int)statusCode > 599))
                    statusCode = HttpStatusCode.BadGateway;
                var errorMessage = payload?.Message ?? "Không thể kết nối hoặc không có phản hồi từ dịch vụ GHN.";

                throw new GhnApiException(
                    statusCode: statusCode,
                    message: $"Lỗi hệ thống GHN: {errorMessage}",
                    codeMessage: payload?.CodeMessage ?? "GHN_SERVICE_ERROR");
            }

            if (payload.Data is null)
            {
                throw new GhnApiException(
                    statusCode: availableServices ? HttpStatusCode.BadGateway : (HttpStatusCode)payload.Code,
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
                    statusCode: response.IsSuccessStatusCode ? HttpStatusCode.BadGateway : response.StatusCode,
                    message: "GHN trả về dữ liệu không đúng định dạng JSON.",
                    codeMessage: "JSON_PARSING_ERROR",
                    innerException: exception);
            }

            if (!response.IsSuccessStatusCode || payload is null || payload.Code != 200)
            {
                var statusCode = !response.IsSuccessStatusCode ? response.StatusCode :
                    payload?.Code is >= 400 and <= 599 ? (HttpStatusCode)payload.Code : HttpStatusCode.BadGateway;
                var errorMessage = payload?.Message ?? "Không thể kết nối hoặc không có phản hồi từ dịch vụ GHN.";

                throw new GhnApiException(
                    statusCode: statusCode,
                    message: $"Lỗi hệ thống GHN: {errorMessage}",
                    codeMessage: payload?.CodeMessage ?? "GHN_SERVICE_ERROR");
            }

            if (payload.Data is null)
            {
                throw new GhnApiException(
                    statusCode: HttpStatusCode.BadGateway,
                    message: payload.Message ?? "GHN trả về trạng thái thành công nhưng dữ liệu bị rỗng (null).",
                    codeMessage: payload.CodeMessage ?? "EMPTY_DATA_ERROR");
            }

            return payload.Data;
        }

        public async Task<GhnFeeQuoteResponse> GetShippingFeeAsync(CalculateGhnFeeRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var apiRequest = MapFeeRequest(request);

            // endpoint của GHN tính phí ship: "shipping-order/fee"
            var response = await SendSingleAsync<GhnCalculateFeeData>(
                HttpMethod.Post,
                "v2/shipping-order/fee",
                apiRequest,
                cancellationToken);

            return new GhnFeeQuoteResponse(
                 TotalFee: response.Total,
                 Breakdown: new GhnFeeBreakdownSnapshotDto
                 {
                     ServiceFee = response.ServiceFee,
                     InsuranceFee = response.InsuranceFee,
                     PickStationFee = response.PickStationFee,
                     CouponValue = response.CouponValue,
                     R2sFee = response.R2sFee,
                     DocumentReturnFee = response.DocumentReturnFee,
                     DoubleCheckFee = response.DoubleCheckFee,
                     CodFee = response.CodFee,
                     PickRemoteAreasFee = response.PickRemoteAreasFee,
                     DeliverRemoteAreasFee = response.DeliverRemoteAreasFee,
                     CodFailedFee = response.CodFailedFee
                 });
        }

        private static GhnCalculateFeeApiRequest MapFeeRequest(CalculateGhnFeeRequest request)
        {
            if (request.FromDistrictId <= 0 ||
                string.IsNullOrWhiteSpace(request.FromWardCode))
            {
                throw new ArgumentException("Địa chỉ người gửi không hợp lệ.", nameof(request));
            }

            if (request.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(request.ToWardCode))
            {
                throw new ArgumentException("Địa chỉ người nhận không hợp lệ.", nameof(request));
            }

            if (request.WeightGram <= 0)
            {
                throw new ArgumentException("Khối lượng phải lớn hơn 0.", nameof(request));
            }

            var isLight = request.ServiceTypeId == 2;
            var isHeavy = request.ServiceTypeId == 5;

            if (!isLight && !isHeavy)
            {
                throw new ArgumentException("ServiceTypeId chỉ nhận 2 hoặc 5.", nameof(request));
            }

            if (isLight &&
                (request.LengthCm is null or <= 0 ||
                 request.WidthCm is null or <= 0 ||
                 request.HeightCm is null or <= 0))
            {
                throw new ArgumentException("Hàng nhẹ phải có đầy đủ dài, rộng và cao.", nameof(request));
            }

            if (isHeavy && request.Items.Count == 0)
            {
                throw new ArgumentException("Hàng nặng phải có ít nhất một kiện hàng.", nameof(request));
            }

            return new GhnCalculateFeeApiRequest
            {
                FromDistrictId = request.FromDistrictId,
                FromWardCode = request.FromWardCode.Trim(),

                ToDistrictId = request.ToDistrictId,
                ToWardCode = request.ToWardCode.Trim(),

                ServiceTypeId = request.ServiceTypeId,
                WeightGram = request.WeightGram,

                LengthCm = isLight ? request.LengthCm : null,
                WidthCm = isLight ? request.WidthCm : null,
                HeightCm = isLight ? request.HeightCm : null,

                InsuranceValue = 0,
                CodValue = 0,
                //CodFailedAmount = 0,
                Coupon = null,

                Items = isHeavy
                    ? request.Items.Select(MapItem).ToList()
                    : null
            };
        }

        public async Task<GhnPreviewQuote> PreviewOrderAsync(GhnShippingPreviewRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var apiRequest = MapPreviewRequest(request);

            // endpoint preview của GHN: "v2/shipping-order/preview"
            var data = await SendSingleAsync<GhnPreviewOrderData>(
                HttpMethod.Post,
                "v2/shipping-order/preview",
                apiRequest,
                cancellationToken);

            return new GhnPreviewQuote(
                TotalFee: data.TotalFee,
                ExpectedDeliveryAt: ParseExpectedDelivery(data.ExpectedDeliveryTime));
        }

        public async Task<GhnOrderDetailResponse> GetOrderDetailAsync(string ghnOrderCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ghnOrderCode))
            {
                throw new ArgumentException(
                    "Mã vận đơn GHN không được để trống.",
                    nameof(ghnOrderCode));
            }

            var normalizedOrderCode = ghnOrderCode.Trim();

            if (normalizedOrderCode.Length > 50)
            {
                throw new ArgumentException(
                    "Mã vận đơn GHN không được vượt quá 50 ký tự.",
                    nameof(ghnOrderCode));
            }

            var data = await SendSingleAsync<GhnOrderDetailData>(
                 HttpMethod.Get,
                 "v2/shipping-order/detail?order_code=" + Uri.EscapeDataString(normalizedOrderCode),
                 null,
                 cancellationToken);

            //var data = await SendSingleAsync<GhnOrderDetailData>(
            //    HttpMethod.Post,
            //    "v2/shipping-order/detail",
            //    new GhnOrderDetailApiRequest(normalizedOrderCode),
            //    cancellationToken);

            if (data is null)
            {
                throw new GhnApiException(
                    HttpStatusCode.BadGateway,
                    "GHN báo thành công nhưng không trả chi tiết vận đơn.",
                    "EMPTY_ORDER_DETAIL");
            }

            if (string.IsNullOrWhiteSpace(data.OrderCode))
            {
                throw new GhnApiException(
                    HttpStatusCode.BadGateway,
                    "GHN trả chi tiết vận đơn nhưng thiếu mã vận đơn.",
                    "EMPTY_ORDER_CODE");
            }

            if (!string.Equals(
                    data.OrderCode.Trim(),
                    normalizedOrderCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new GhnApiException(
                    HttpStatusCode.BadGateway,
                    "Mã vận đơn GHN trả về không khớp với mã được yêu cầu.",
                    "ORDER_CODE_MISMATCH");
            }

            var timeline = (data.Log ?? new List<GhnOrderDetailLogData>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Status))
                .Select(x => new GhnTrackingLogResponse
                {
                    Status = x.Status!.Trim().ToLowerInvariant(),
                    OccurredAt = x.UpdatedDate
                })
                .GroupBy(x => new
                {
                    Status = x.Status.ToLowerInvariant(),
                    x.OccurredAt
                })
                .Select(group => group.First())
                .OrderBy(x => x.OccurredAt ?? DateTimeOffset.MaxValue)
                .ToArray();

            var carrierStatus = string.IsNullOrWhiteSpace(data.Status)
                ? "unknown"
                : data.Status.Trim().ToLowerInvariant();

            return new GhnOrderDetailResponse
            {
                OrderCode = data.OrderCode.Trim(),
                ClientOrderCode = NormalizeOptionalText(data.ClientOrderCode),

                CarrierStatus = carrierStatus,
                ServiceTypeId = data.ServiceTypeId,

                WeightGram = data.WeightGram,
                ConvertedWeightGram = data.ConvertedWeightGram,
                CalculateWeightGram = data.CalculateWeightGram,
                LengthCm = data.LengthCm,
                WidthCm = data.WidthCm,
                HeightCm = data.HeightCm,

                RequiredNote = NormalizeOptionalText(data.RequiredNote),
                Content = NormalizeOptionalText(data.Content),
                Note = NormalizeOptionalText(data.Note),

                ExpectedDeliveryAt = data.Leadtime,
                OrderCreatedAt = data.CreatedDate ?? ParseGhnDetailDate(data.OrderDate),
                FinishedAt = data.FinishDate,
                CarrierUpdatedAt = ParseGhnDetailDate(data.UpdatedDate),

                Timeline = timeline
            };
        }

        private static GhnPreviewOrderApiRequest MapPreviewRequest(GhnShippingPreviewRequest request)
        {
            var validation = new HomeCycle.Application.Validations.GHN.GhnShippingPreviewRequestValidator().Validate(request);
            if (!validation.IsValid)
                throw new ArgumentException(string.Join(", ", validation.Errors.Select(x => x.ErrorMessage)), nameof(request));
            if (request.WeightGram is null or <= 0 || request.LengthCm is null or <= 0 ||
                request.WidthCm is null or <= 0 || request.HeightCm is null or <= 0)
                throw new ArgumentException("Preview phải có đủ khối lượng và kích thước cấp đơn.", nameof(request));
            if (request.ServiceTypeId == 5 && (request.Items.Count == 0 ||
                (request.WeightGram < 20_000 && request.ParcelCount == 1)))
                throw new ArgumentException("Type 5 cần items và tổng cân từ 20kg hoặc nhiều kiện.", nameof(request));

            var sender = request.Sender!;
            var receiver = request.Receiver!;
            var fromAddress = BuildAddressText(sender.Address);
            var toAddress = BuildAddressText(receiver.Address);
            if (fromAddress.Length > 1024 || toAddress.Length > 1024)
                throw new ArgumentException("Địa chỉ đầy đủ không được vượt quá 1024 ký tự.", nameof(request));

            return new GhnPreviewOrderApiRequest
            {
                FromName = sender.FullName.Trim(),
                FromPhone = sender.Phone.Trim(),
                FromAddress = fromAddress,
                FromWardName = sender.Address.WardName.Trim(),
                FromDistrictName = sender.Address.DistrictName.Trim(),
                FromProvinceName = sender.Address.ProvinceName.Trim(),
                ToName = receiver.FullName.Trim(),
                ToPhone = receiver.Phone.Trim(),
                ToAddress = toAddress,
                ToWardName = receiver.Address.WardName.Trim(),
                ToDistrictName = receiver.Address.DistrictName.Trim(),
                ToProvinceName = receiver.Address.ProvinceName.Trim(),
                ToWardCode = receiver.Address.WardCode.Trim(),
                ToDistrictId = receiver.Address.DistrictId,
                Content = string.IsNullOrWhiteSpace(request.Content) ? "Sản phẩm HomeCycle" : request.Content.Trim(),
                ServiceTypeId = request.ServiceTypeId,
                PaymentTypeId = 1,
                RequiredNote = request.RequiredNote!.Trim().ToUpperInvariant(),
                WeightGram = request.WeightGram,
                LengthCm = request.LengthCm,
                WidthCm = request.WidthCm,
                HeightCm = request.HeightCm,
                Items = request.Items.Count == 0 ? null : request.Items.Select(MapPreviewItem).ToList()
            };
        }
        private static GhnApiItemRequest MapPreviewItem(CalculateGhnFeeItemRequest item)
        {
            // quy ước: cạnh lớn nhất là dài, nhỏ nhất là cao.
            var sides = new[]
            {
                item.LengthCm,
                item.WidthCm,
                item.HeightCm
            }
            .OrderByDescending(x => x)
            .ToArray();

            return new GhnApiItemRequest
            {
                Name = item.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(item.Code) ? null : item.Code.Trim(),
                Quantity = item.Quantity,
                WeightGram = item.WeightGram,
                LengthCm = sides[0],
                WidthCm = sides[1],
                HeightCm = sides[2]
            };
        }

        private static string BuildAddressText(GhnAddressSnapshotDto address)
        {
            var parts = new[]
            {
                address.AddressDetail,
                address.WardName,
                address.DistrictName,
                address.ProvinceName
            }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

            return string.Join(", ", parts);
        }

        private static DateTimeOffset? ParseExpectedDelivery(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTimeOffset.TryParseExact(
                    value,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var exact))
            {
                return exact;
            }

            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }

        public async Task<GhnCreateOrderResponse> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var apiRequest = MapCreateOrderRequest(request);
            var services = await GetAvailableServicesAsync(request.FromDistrictId, request.ToDistrictId, cancellationToken);
            if (!services.Any(x => x.ServiceTypeId == request.ServiceTypeId))
                throw new ArgumentException("Dịch vụ GHN không khả dụng cho tuyến này.");

            var data = await SendSingleAsync<GhnCreateOrderData>(
                HttpMethod.Post,
                "v2/shipping-order/create",
                apiRequest,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(data.OrderCode))
            {
                throw new GhnApiException(
                    HttpStatusCode.BadGateway,
                    "GHN báo tạo đơn thành công nhưng không trả mã vận đơn.",
                    "EMPTY_ORDER_CODE");
            }

            return new GhnCreateOrderResponse(
                OrderCode: data.OrderCode,
                TotalFee: data.TotalFee,
                ServiceFee: data.Fee?.MainService ?? 0m,
                CodFee: data.Fee?.CodFee ?? 0m,
                ExpectedDeliveryAt: data.ExpectedDeliveryTime);
        }

        private static GhnCalculateFeeItemApiRequest MapItem(CalculateGhnFeeItemRequest item)
        {
            // quy ước: cạnh lớn nhất là dài, nhỏ nhất là cao.
            var sides = new[]
            {
                item.LengthCm,
                item.WidthCm,
                item.HeightCm
            }

            .OrderByDescending(x => x)
            .ToArray();

            return new GhnCalculateFeeItemApiRequest
            {
                Name = item.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(item.Code)
                    ? null
                    : item.Code.Trim(),
                Quantity = item.Quantity,
                WeightGram = item.WeightGram,
                LengthCm = sides[0],
                WidthCm = sides[1],
                HeightCm = sides[2]
            };
        }

        private static GhnCreateOrderApiRequest MapCreateOrderRequest(GhnCreateOrderRequest request)
        {
            static string Required(string? value, int max, string name)
            {
                if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
                    throw new ArgumentException($"{name} bắt buộc và tối đa {max} ký tự.");
                return value.Trim();
            }
            var clientCode = Required(request.ClientOrderCode, 50, nameof(request.ClientOrderCode));
            if (request.FromDistrictId <= 0 || request.ToDistrictId <= 0)
                throw new ArgumentException("Cần quận/huyện người gửi và người nhận để kiểm tra dịch vụ.");
            if (request.WeightGram is null or < 1 or > 50_000 || request.LengthCm is null or < 1 or > 200 ||
                request.WidthCm is null or < 1 or > 200 || request.HeightCm is null or < 1 or > 200 || request.ParcelCount < 1)
                throw new ArgumentException("Thiếu hoặc sai thông số đóng gói cấp đơn (1–50.000g, 1–200cm).");
            var expectedType = request.WeightGram >= 20_000 || request.ParcelCount > 1 ? 5 : 2;
            if (request.ServiceTypeId != expectedType) throw new ArgumentException("ServiceTypeId không khớp cân nặng/số kiện.");
            if (request.PaymentTypeId != 1 || request.CodAmount != 0)
                throw new ArgumentException("HomeCycle dùng shop trả phí và COD = 0.");
            if (request.InsuranceValue is < 0 or > 5_000_000 || request.Note?.Length > 5000 || request.Content?.Length > 2000)
                throw new ArgumentException("Khai giá, note hoặc content vượt giới hạn GHN.");
            var note = Required(request.RequiredNote, 30, nameof(request.RequiredNote)).ToUpperInvariant();
            if (!new[] { "CHOTHUHANG", "CHOXEMHANGKHONGTHU", "KHONGCHOXEMHANG" }.Contains(note))
                throw new ArgumentException("RequiredNote không hợp lệ.");
            var items = request.Items ?? Array.Empty<GhnCreateOrderItemRequest>();
            if ((expectedType == 5 || string.IsNullOrWhiteSpace(request.Content)) && items.Count == 0)
                throw new ArgumentException("Cần items cho hàng nặng; hàng nhẹ cần content hoặc items.");
            long total = 0;
            var mapped = new List<GhnCreateOrderApiItem>();
            foreach (var item in items)
            {
                if (item == null || item.Quantity < 1 || item.WeightGram < 0 || item.LengthCm < 0 || item.WidthCm < 0 || item.HeightCm < 0)
                    throw new ArgumentException("Item không hợp lệ.");
                if (expectedType == 5 && (item.WeightGram < 1 || item.LengthCm is < 1 or > 200 || item.WidthCm is < 1 or > 200 || item.HeightCm is < 1 or > 200))
                    throw new ArgumentException("Hàng nặng cần thông số từng kiện hợp lệ.");
                total = checked(total + (long)item.WeightGram * item.Quantity);
                mapped.Add(new GhnCreateOrderApiItem { Name = Required(item.Name, 512, "Item.Name"), Code = item.Code?.Trim(),
                    Quantity = item.Quantity, WeightGram = item.WeightGram, LengthCm = item.LengthCm, WidthCm = item.WidthCm, HeightCm = item.HeightCm });
            }
            if (expectedType == 5 && total != request.WeightGram) throw new ArgumentException("Tổng cân không khớp items.");
            return new GhnCreateOrderApiRequest
            {
                ClientOrderCode = clientCode,
                FromName = Required(request.FromName,1024,"FromName"), FromPhone = Required(request.FromPhone,1024,"FromPhone"),
                FromAddress = Required(request.FromAddress,1024,"FromAddress"), FromWardName = Required(request.FromWardName,1024,"FromWardName"),
                FromDistrictName = Required(request.FromDistrictName,1024,"FromDistrictName"), FromProvinceName = Required(request.FromProvinceName,1024,"FromProvinceName"),
                ToName = Required(request.ToName,1024,"ToName"), ToPhone = Required(request.ToPhone,1024,"ToPhone"),
                ToAddress = Required(request.ToAddress,1024,"ToAddress"), ToWardName = Required(request.ToWardName,1024,"ToWardName"),
                ToDistrictName = Required(request.ToDistrictName,1024,"ToDistrictName"), ToProvinceName = Required(request.ToProvinceName,1024,"ToProvinceName"),
                ToDistrictId = request.ToDistrictId, ToWardCode = Required(request.ToWardCode,1024,"ToWardCode"),
                ServiceTypeId = request.ServiceTypeId, PaymentTypeId = 1, CodAmount = 0, InsuranceValue = request.InsuranceValue,
                RequiredNote = note, Note = request.Note?.Trim(), Content = request.Content?.Trim(),
                WeightGram = request.WeightGram.Value, LengthCm = request.LengthCm, WidthCm = request.WidthCm, HeightCm = request.HeightCm,
                Items = mapped
            };
        }
        private static DateTimeOffset? ParseGhnDetailDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value.Trim(), "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return DateTimeOffset.TryParse(
                value.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var parsed)
                    ? parsed
                    : null;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

    }
}
