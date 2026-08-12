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
                CodFailedAmount = 0,
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

        private static GhnPreviewOrderApiRequest MapPreviewRequest(GhnShippingPreviewRequest request)
        {
            var sender = request.Sender;
            var receiver = request.Receiver;

            if (sender is null || sender.Address is null)
                throw new ArgumentException("Thiếu thông tin người gửi (Sender).", nameof(request));

            if (receiver is null || receiver.Address is null)
                throw new ArgumentException("Thiếu thông tin người nhận (Receiver).", nameof(request));

            if (sender.Address.DistrictId <= 0 || string.IsNullOrWhiteSpace(sender.Address.WardCode))
                throw new ArgumentException("Địa chỉ người gửi không hợp lệ (DistrictId/WardCode).", nameof(request));

            if (receiver.Address.DistrictId <= 0 || string.IsNullOrWhiteSpace(receiver.Address.WardCode))
                throw new ArgumentException("Địa chỉ người nhận không hợp lệ (DistrictId/WardCode).", nameof(request));

            var isLight = request.ServiceTypeId == 2;
            var isHeavy = request.ServiceTypeId == 5;

            if (!isLight && !isHeavy)
                throw new ArgumentException("ServiceTypeId chỉ nhận 2 hoặc 5.", nameof(request));

            string[] allowedRequiredNotes = ["CHOTHUHANG", "CHOXEMHANGKHONGTHU", "KHONGCHOXEMHANG"];
            if (string.IsNullOrWhiteSpace(request.RequiredNote) ||
                !allowedRequiredNotes.Contains(request.RequiredNote.Trim(), StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("RequiredNote không hợp lệ.", nameof(request));

            if (isLight)
            {
                if (request.WeightGram is null or <= 0 ||
                    request.LengthCm is null or <= 0 ||
                    request.WidthCm is null or <= 0 ||
                    request.HeightCm is null or <= 0)
                {
                    throw new ArgumentException("Hàng nhẹ phải có đầy đủ khối lượng và kích thước.", nameof(request));
                }

                return new GhnPreviewOrderApiRequest
                {
                    FromName = sender.FullName.Trim(),
                    FromPhone = sender.Phone.Trim(),
                    FromAddress = BuildAddressText(sender.Address),
                    FromWardName = sender.Address.WardName?.Trim() ?? string.Empty,
                    FromDistrictName = sender.Address.DistrictName?.Trim() ?? string.Empty,
                    FromProvinceName = string.IsNullOrWhiteSpace(sender.Address.ProvinceName)
                        ? null
                        : sender.Address.ProvinceName.Trim(),

                    ToName = receiver.FullName.Trim(),
                    ToPhone = receiver.Phone.Trim(),
                    ToAddress = BuildAddressText(receiver.Address),
                    ToWardCode = receiver.Address.WardCode.Trim(),
                    ToDistrictId = receiver.Address.DistrictId,

                    ServiceTypeId = request.ServiceTypeId,
                    PaymentTypeId = 1,
                    RequiredNote = request.RequiredNote.Trim().ToUpperInvariant(),

                    WeightGram = request.WeightGram,
                    LengthCm = request.LengthCm,
                    WidthCm = request.WidthCm,
                    HeightCm = request.HeightCm,

                    Items = null
                };
            }

            if (request.Items.Count == 0)
                throw new ArgumentException("Hàng nặng phải có ít nhất một kiện hàng.", nameof(request));

            return new GhnPreviewOrderApiRequest
            {
                FromName = sender.FullName.Trim(),
                FromPhone = sender.Phone.Trim(),
                FromAddress = BuildAddressText(sender.Address),
                FromWardName = sender.Address.WardName?.Trim() ?? string.Empty,
                FromDistrictName = sender.Address.DistrictName?.Trim() ?? string.Empty,
                FromProvinceName = string.IsNullOrWhiteSpace(sender.Address.ProvinceName)
                    ? null
                    : sender.Address.ProvinceName.Trim(),

                ToName = receiver.FullName.Trim(),
                ToPhone = receiver.Phone.Trim(),
                ToAddress = BuildAddressText(receiver.Address),
                ToWardCode = receiver.Address.WardCode.Trim(),
                ToDistrictId = receiver.Address.DistrictId,

                ServiceTypeId = request.ServiceTypeId,
                PaymentTypeId = 1,
                RequiredNote = request.RequiredNote.Trim().ToUpperInvariant(),

                WeightGram = null,
                LengthCm = null,
                WidthCm = null,
                HeightCm = null,

                Items = request.Items.Select(MapPreviewItem).ToList()
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
            // 1. Giữ nguyên các Guard Clauses kiểm tra điều kiện đầu vào của bạn
            if (string.IsNullOrWhiteSpace(request.ClientOrderCode) || request.ClientOrderCode.Length > 50)
                throw new ArgumentException("ClientOrderCode bắt buộc và không được vượt quá 50 ký tự.", nameof(request));

            if (request.ServiceTypeId is not (2 or 5))
                throw new ArgumentException("ServiceTypeId chỉ nhận 2 hoặc 5.", nameof(request));

            if (request.PaymentTypeId != 1 || request.CodAmount != 0)
                throw new ArgumentException("Đơn HomeCycle phải có PaymentTypeId = 1 và CodAmount = 0.", nameof(request));

            if (request.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(request.ToWardCode))
                throw new ArgumentException("Địa chỉ người nhận không hợp lệ: thiếu ToDistrictId hoặc ToWardCode.", nameof(request));

            string[] allowedRequiredNotes = ["CHOTHUHANG", "CHOXEMHANGKHONGTHU", "KHONGCHOXEMHANG"];
            if (!allowedRequiredNotes.Contains(request.RequiredNote, StringComparer.Ordinal))
                throw new ArgumentException("RequiredNote không hợp lệ.", nameof(request));

            bool isLight = request.ServiceTypeId == 2;

            if (isLight && (request.WeightGram is null or <= 0 || request.LengthCm is null or <= 0 || request.WidthCm is null or <= 0 || request.HeightCm is null or <= 0))
                throw new ArgumentException("Hàng nhẹ phải có đủ khối lượng và kích thước.", nameof(request));

            if (isLight && (request.WeightGram > 50_000 || request.LengthCm > 200 || request.WidthCm > 200 || request.HeightCm > 200))
                throw new ArgumentException("Kích thước hoặc khối lượng hàng nhẹ vượt giới hạn GHN.", nameof(request));

            if (!isLight && request.Items.Count == 0)
                throw new ArgumentException("Hàng nặng phải có ít nhất một kiện hàng.", nameof(request));

            // 2. CHUẨN HÓA MẢNG ITEMS (GHN bắt buộc phải có cho mọi ServiceType)
            List<GhnCreateOrderApiItem> apiItems;

            if (isLight)
            {
                // Hàng nhẹ (ServiceType 2): Tự sinh một item đại diện bằng thông tin kích thước tổng
                int[] totalSides = [request.LengthCm!.Value, request.WidthCm!.Value, request.HeightCm!.Value];
                Array.Sort(totalSides);
                Array.Reverse(totalSides); // Cạnh lớn nhất làm chiều dài

                apiItems = new List<GhnCreateOrderApiItem>
        {
            new() {
                Name = !string.IsNullOrWhiteSpace(request.Content) ? request.Content.Trim() : "Sản phẩm HomeCycle",
                Quantity = 1,
                WeightGram = request.WeightGram!.Value,
                LengthCm = totalSides[0],
                WidthCm = totalSides[1],
                HeightCm = totalSides[2]
            }
        };
            }
            else
            {
                // Hàng nặng (ServiceType 5): Duyệt mảng item đầu vào của bạn
                apiItems = request.Items.Select(item =>
                {
                    if (string.IsNullOrWhiteSpace(item.Name) || item.Quantity <= 0 || item.WeightGram <= 0 || item.LengthCm <= 0 || item.WidthCm <= 0 || item.HeightCm <= 0)
                        throw new ArgumentException("Thông tin kiện hàng GHN không hợp lệ.", nameof(request));

                    int[] itemSides = [item.LengthCm, item.WidthCm, item.HeightCm];
                    Array.Sort(itemSides);
                    Array.Reverse(itemSides);

                    return new GhnCreateOrderApiItem
                    {
                        Name = item.Name.Trim(),
                        Code = item.Code?.Trim(),
                        Quantity = item.Quantity,
                        WeightGram = item.WeightGram,
                        LengthCm = itemSides[0],
                        WidthCm = itemSides[1],
                        HeightCm = itemSides[2]
                    };
                }).ToList();
            }

            // 3. TÍNH TOÁN CÂN NẶNG VÀ KÍCH THƯỚC TỔNG CẤP ĐƠN HÀNG (Bắt buộc không được để trống/null)
            int finalWeight = isLight
                ? request.WeightGram!.Value
                : request.Items.Sum(x => x.WeightGram * x.Quantity);

            // Đối với hàng nặng, nếu bạn không truyền kích thước tổng, hãy lấy kích thước của item lớn nhất để GHN không bắt lỗi trống trường
            int finalLength = isLight ? request.LengthCm!.Value : request.Items.Max(x => x.LengthCm);
            int finalWidth = isLight ? request.WidthCm!.Value : request.Items.Max(x => x.WidthCm);
            int finalHeight = isLight ? request.HeightCm!.Value : request.Items.Max(x => x.HeightCm);

            return new GhnCreateOrderApiRequest
            {
                ClientOrderCode = request.ClientOrderCode,
                FromName = request.FromName.Trim(),
                FromPhone = request.FromPhone.Trim(),
                FromAddress = request.FromAddress.Trim(),
                FromWardName = request.FromWardName.Trim(),
                FromDistrictName = request.FromDistrictName.Trim(),
                FromProvinceName = request.FromProvinceName.Trim(),

                ToName = request.ToName.Trim(),
                ToPhone = request.ToPhone.Trim(),
                ToAddress = request.ToAddress.Trim(),
                ToDistrictId = request.ToDistrictId,
                ToWardCode = request.ToWardCode.Trim(),

                ServiceTypeId = request.ServiceTypeId,
                PaymentTypeId = 1, // Luôn là shop trả phí
                CodAmount = 0,     // Luôn không thu hộ qua GHN
                InsuranceValue = request.InsuranceValue,
                RequiredNote = request.RequiredNote,
                Note = request.Note?.Trim(),
                Content = request.Content?.Trim(),

                // Cập nhật giá trị số bắt buộc cho cấp đơn hàng, không để null
                WeightGram = finalWeight,
                LengthCm = finalLength,
                WidthCm = finalWidth,
                HeightCm = finalHeight,

                Items = apiItems
            };
        }

    }
}
