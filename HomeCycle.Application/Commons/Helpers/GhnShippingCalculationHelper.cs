using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public static class GhnShippingCalculationHelper
    {
        public static bool IsGhnDelivery(HomeCycle.Domain.Enums.DeliveryMethod? deliveryMethod) =>
            deliveryMethod == HomeCycle.Domain.Enums.DeliveryMethod.GhnDelivery;

        public static bool IsAgreementGhnDelivery(HomeCycle.Domain.Enums.AgreementType? agreementType,
            HomeCycle.Domain.Enums.DeliveryMethod? deliveryMethod) =>
            agreementType == HomeCycle.Domain.Enums.AgreementType.No_Inspection && IsGhnDelivery(deliveryMethod);

        public static string SnapshotHash(GhnShippingInfo info)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                info.Sender, info.Receiver, info.ServiceTypeId, info.ParcelCount,
                Parcel = GetConfirmedParcel(info), info.Items,
                RequiredNote = info.RequiredNote?.Trim().ToUpperInvariant(),
                Content = string.IsNullOrWhiteSpace(info.Content) ? "Sản phẩm HomeCycle" : info.Content
            });
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }

        // Separate purpose from access tokens; no new database/configuration is required.
        public static string IssuePreviewToken(string scope, GhnShippingInfo info, GhnQuoteSnapshotDto quote, string secret)
        {
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Thiếu khóa xác nhận preview GHN.");
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(
                new PreviewProof(scope, SnapshotHash(info), quote))));
            var signature = System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes("HomeCycle.GhnPreview.v1:" + payload));
            return payload + "." + Convert.ToBase64String(signature);
        }

        public static GhnQuoteSnapshotDto ConfirmPreview(string scope, GhnShippingInfo info, string secret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Thiếu khóa xác nhận preview GHN.");
                var parts = (info.PreviewToken ?? string.Empty).Split('.');
                if (parts.Length != 2) throw new ArgumentException("Cần preview GHN trước khi xác nhận.");
                var expected = System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
                    Encoding.UTF8.GetBytes("HomeCycle.GhnPreview.v1:" + parts[0]));
                if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(parts[1])))
                    throw new ArgumentException("Mã xác nhận preview GHN không hợp lệ.");
                var proof = System.Text.Json.JsonSerializer.Deserialize<PreviewProof>(Convert.FromBase64String(parts[0]));
                if (proof == null || proof.Scope != scope || proof.Hash != SnapshotHash(info) ||
                    proof.Quote.InputHash != proof.Hash)
                    throw new ArgumentException("Snapshot đã thay đổi; vui lòng preview GHN lại.");
                if (proof.Quote.QuotedAt > DateTimeOffset.UtcNow || proof.Quote.QuotedAt.AddMinutes(30) <= DateTimeOffset.UtcNow)
                    throw new ArgumentException("Preview GHN đã hết hạn; vui lòng preview lại.");
                return proof.Quote;
            }
            catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
            {
                throw new ArgumentException("Mã xác nhận preview GHN không hợp lệ.", ex);
            }
        }

        private sealed record PreviewProof(string Scope, string Hash, GhnQuoteSnapshotDto Quote);

        public const int HeavyThresholdGram = 20_000;
        public const int MaxShipmentWeightGram = 50_000;

        // HomeCycle mapping convention: one item is ONE physical parcel, never Product quantity.
        public static int DetermineServiceType(long totalWeight, int parcelCount) =>
            totalWeight >= HeavyThresholdGram || parcelCount > 1 ? 5 : 2;

        public static IReadOnlyList<GhnItemSnapshotDto>? ToParcelItems(IReadOnlyList<CalculateGhnFeeItemRequest>? items) =>
            items?.Select(x => x == null ? null! : new GhnItemSnapshotDto
            {
                Name = x.Name, Code = x.Code, Quantity = x.Quantity, WeightGram = x.WeightGram,
                LengthCm = x.LengthCm, WidthCm = x.WidthCm, HeightCm = x.HeightCm
            }).ToArray();

        public static Error? ValidatePhysicalParcels(IReadOnlyList<GhnItemSnapshotDto>? items,
            int parcelCount, int? rootWeight, int? serviceType)
        {
            if (items == null || items.Count == 0 || parcelCount < 1)
                return new Error("Ghn.InvalidParcel", "Cần ít nhất một kiện vật lý.");
            if (parcelCount != items.Count)
                return new Error("Ghn.ParcelCountMismatch", "Số kiện phải bằng số phần tử Items.");
            if (items.Any(x => x == null || string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 512 ||
                x.Quantity != 1 || x.WeightGram <= 0 || x.LengthCm is < 1 or > 200 ||
                x.WidthCm is < 1 or > 200 || x.HeightCm is < 1 or > 200))
                return new Error("Ghn.InvalidParcel", "Mỗi kiện cần tên, Quantity = 1, cân nặng > 0 và mỗi chiều từ 1 đến 200cm.");
            long total = items.Sum(x => (long)x.WeightGram);
            var expectedType = DetermineServiceType(total, parcelCount);
            if (total > MaxShipmentWeightGram)
                return new Error("Ghn.WeightLimitExceeded", $"Tổng khối lượng các kiện là {total / 1000m}kg, thuộc Type 5 nhưng vượt giới hạn 50kg của HomeCycle. Vui lòng chọn Người bán giao hoặc Người mua tự lấy.");
            if (!rootWeight.HasValue || rootWeight != total)
                return new Error("Ghn.TotalWeightMismatch", "Root WeightGram phải bằng tổng khối lượng các kiện.");
            if (serviceType != expectedType)
                return new Error("Ghn.InvalidServiceType", $"Thông tin kiện yêu cầu ServiceTypeId = {expectedType}.");
            return null;
        }

        public static Error ParcelError(ArgumentException ex) =>
            new Error(ex.ParamName?.StartsWith("Ghn.", StringComparison.Ordinal) == true
                ? ex.ParamName : "Ghn.InvalidParcel", ex.Message);

        public static GhnLightParcelSnapshotDto GetConfirmedParcel(GhnShippingInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);
            var error = ValidatePhysicalParcels(info.Items, info.ParcelCount, info.WeightGram, info.ServiceTypeId);
            if (error != null) throw new ArgumentException(error.Message, error.Code);

            // TODO: Verify GHN staging root dimensions for multiple physical parcels.
            // Never invent aggregation or accept caller-supplied dimensions as verified policy.
            if (info.ParcelCount > 1)
                throw new ArgumentException("Root dimensions cho nhiều kiện cần được xác minh GHN Staging trước khi gọi tính phí/preview/create.",
                    "Ghn.MultiParcelDimensionsUnverified");

            var single = info.Items[0];
            var length = info.LengthCm ?? single.LengthCm;
            var width = info.WidthCm ?? single.WidthCm;
            var height = info.HeightCm ?? single.HeightCm;
            if (length != single.LengthCm || width != single.WidthCm || height != single.HeightCm)
                throw new ArgumentException("Kích thước cấp đơn phải khớp kiện vật lý duy nhất.", "Ghn.ParcelDimensionsMismatch");
            return new GhnLightParcelSnapshotDto
            {
                WeightGram = single.WeightGram, LengthCm = length, WidthCm = width, HeightCm = height
            };
        }

        private static readonly HashSet<string> ValidRequiredNotes = new(StringComparer.OrdinalIgnoreCase)
        {
            "CHOTHUHANG",
            "CHOXEMHANGKHONGTHU",
            "KHONGCHOXEMHANG"
        };

        public static Result<CalculateGhnFeeRequest> BuildFeeRequest(
            GhnShippingInfo info,
            product? product)
        {
            var sender = info.Sender;
            var receiver = info.Receiver;
            var senderAddress = sender?.Address;
            var receiverAddress = receiver?.Address;

            if (sender == null || receiver == null)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.ContactRequired", "Cần đầy đủ thông tin người gửi và người nhận."));
            }

            if (string.IsNullOrWhiteSpace(sender.FullName)
                || string.IsNullOrWhiteSpace(sender.Phone)
                || string.IsNullOrWhiteSpace(receiver.FullName)
                || string.IsNullOrWhiteSpace(receiver.Phone))
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.ContactRequired", "Thiếu tên hoặc số điện thoại người gửi/người nhận."));
            }

            if (senderAddress == null
                || receiverAddress == null
                || senderAddress.DistrictId <= 0
                || receiverAddress.DistrictId <= 0
                || string.IsNullOrWhiteSpace(senderAddress.WardCode)
                || string.IsNullOrWhiteSpace(receiverAddress.WardCode)
                || string.IsNullOrWhiteSpace(senderAddress.AddressDetail)
                || string.IsNullOrWhiteSpace(receiverAddress.AddressDetail))
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.AddressRequired", "Thiếu địa chỉ GHN hợp lệ của người gửi hoặc người nhận."));
            }

            if (info.ServiceTypeId is not (2 or 5))
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.InvalidServiceType", "ServiceTypeId GHN chỉ nhận 2 hoặc 5."));
            }

            if (string.IsNullOrWhiteSpace(info.RequiredNote)
                || !ValidRequiredNotes.Contains(info.RequiredNote.Trim()))
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.InvalidRequiredNote", "RequiredNote GHN không hợp lệ."));
            }

            try
            {
                var parcel = GetConfirmedParcel(info);
                return Result<CalculateGhnFeeRequest>.Success(new CalculateGhnFeeRequest
                {
                    ParcelCount = info.ParcelCount,
                    FromDistrictId = senderAddress.DistrictId, FromWardCode = senderAddress.WardCode,
                    ToDistrictId = receiverAddress.DistrictId, ToWardCode = receiverAddress.WardCode,
                    ServiceTypeId = info.ServiceTypeId.Value, WeightGram = parcel.WeightGram,
                    LengthCm = parcel.LengthCm, WidthCm = parcel.WidthCm, HeightCm = parcel.HeightCm,
                    Items = (info.Items ?? Array.Empty<GhnItemSnapshotDto>()).Select(x => new CalculateGhnFeeItemRequest
                    { Name = x.Name, Code = x.Code, Quantity = x.Quantity, WeightGram = x.WeightGram,
                      LengthCm = x.LengthCm, WidthCm = x.WidthCm, HeightCm = x.HeightCm }).ToArray()
                });
            }
            catch (Exception ex) when (ex is ArgumentException or OverflowException)
            {
                return Result<CalculateGhnFeeRequest>.Fail(ex is ArgumentException argument
                    ? ParcelError(argument) : new Error("Ghn.WeightLimitExceeded", "Tổng cân kiện vượt phạm vi hỗ trợ."));
            }
        }
    }
}
