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

            if (info.ServiceTypeId == 2)
                return BuildLightGoodsRequest(info, product);

            return BuildHeavyGoodsRequest(info);
        }

        private static Result<CalculateGhnFeeRequest> BuildLightGoodsRequest(
            GhnShippingInfo info,
            product? product)
        {
            if (product == null)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Product.NotFound", "Không tìm thấy sản phẩm của đơn hàng."));
            }

            if (product.Weight is null or <= 0
                || product.Length is null or <= 0
                || product.Width is null or <= 0
                || product.Height is null or <= 0)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.ParcelInformationRequired", "Sản phẩm chưa có đủ khối lượng và kích thước."));
            }

            try
            {
                var sides = new[]
                {
                    checked((int)Math.Ceiling(product.Length.Value)),
                    checked((int)Math.Ceiling(product.Width.Value)),
                    checked((int)Math.Ceiling(product.Height.Value))
                }
                .OrderByDescending(x => x)
                .ToArray();

                var request = new CalculateGhnFeeRequest
                {
                    FromDistrictId = info.Sender!.Address.DistrictId,
                    FromWardCode = info.Sender.Address.WardCode.Trim(),
                    ToDistrictId = info.Receiver!.Address.DistrictId,
                    ToWardCode = info.Receiver.Address.WardCode.Trim(),
                    ServiceTypeId = 2,
                    WeightGram = checked((int)Math.Ceiling(product.Weight.Value * 1000)),
                    LengthCm = sides[0],
                    WidthCm = sides[1],
                    HeightCm = sides[2],
                    Items = Array.Empty<CalculateGhnFeeItemRequest>()
                };

                return Result<CalculateGhnFeeRequest>.Success(request);
            }
            catch (OverflowException)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.ParcelInformationInvalid", "Khối lượng hoặc kích thước sản phẩm vượt phạm vi cho phép."));
            }
        }

        private static Result<CalculateGhnFeeRequest> BuildHeavyGoodsRequest(
            GhnShippingInfo info)
        {
            var items = info.Items?
                .Select(x => new CalculateGhnFeeItemRequest
                {
                    Name = x.Name?.Trim() ?? string.Empty,
                    Code = x.Code,
                    Quantity = x.Quantity,
                    WeightGram = x.WeightGram,
                    LengthCm = x.LengthCm,
                    WidthCm = x.WidthCm,
                    HeightCm = x.HeightCm
                })
                .ToList()
                ?? new List<CalculateGhnFeeItemRequest>();

            if (items.Count == 0)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.HeavyItemsRequired", "Hàng nặng phải có ít nhất một kiện hàng."));
            }

            try
            {
                var totalWeight = items.Aggregate(
                    0L,
                    (total, item) => checked(total + (long)item.WeightGram * item.Quantity));

                if (totalWeight is < 1 or > 1_600_000)
                {
                    return Result<CalculateGhnFeeRequest>.Fail(
                        new Error("Ghn.TotalWeightInvalid", "Tổng khối lượng phải từ 1 đến 1.600.000 gram."));
                }

                var request = new CalculateGhnFeeRequest
                {
                    FromDistrictId = info.Sender!.Address.DistrictId,
                    FromWardCode = info.Sender.Address.WardCode.Trim(),
                    ToDistrictId = info.Receiver!.Address.DistrictId,
                    ToWardCode = info.Receiver.Address.WardCode.Trim(),
                    ServiceTypeId = 5,
                    WeightGram = checked((int)totalWeight),
                    Items = items
                };

                return Result<CalculateGhnFeeRequest>.Success(request);
            }
            catch (OverflowException)
            {
                return Result<CalculateGhnFeeRequest>.Fail(
                    new Error("Ghn.TotalWeightInvalid", "Tổng khối lượng kiện hàng không hợp lệ."));
            }
        }
    }
}
