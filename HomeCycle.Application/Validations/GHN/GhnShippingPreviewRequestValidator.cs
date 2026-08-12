using FluentValidation;
using HomeCycle.Application.DTOs.Requests.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.GHN
{
    public sealed class GhnShippingPreviewRequestValidator : AbstractValidator<GhnShippingPreviewRequest>
    {
        private const int LightGoodsServiceTypeId = 2;
        private const int HeavyGoodsServiceTypeId = 5;

        private const int MaxWeightGram = 1_600_000;
        private const int MaxDimensionCm = 200;

        private static readonly string[] AllowedRequiredNotes =
        [
            "CHOTHUHANG",
            "CHOXEMHANGKHONGTHU",
            "KHONGCHOXEMHANG"
        ];

        public GhnShippingPreviewRequestValidator()
        {
            RuleFor(x => x.Sender)
                .NotNull().WithMessage("Thiếu thông tin người gửi (Sender).");

            RuleFor(x => x.Receiver)
                .NotNull().WithMessage("Thiếu thông tin người nhận (Receiver).");

            When(x => x.Sender != null, () =>
            {
                RuleFor(x => x.Sender!.FullName).NotEmpty().WithMessage("Thiếu tên người gửi.");
                RuleFor(x => x.Sender!.Phone).NotEmpty().WithMessage("Thiếu số điện thoại người gửi.");

                When(x => x.Sender!.Address != null, () =>
                {
                    RuleFor(x => x.Sender!.Address.AddressDetail)
                        .NotEmpty().WithMessage("Thiếu địa chỉ chi tiết người gửi.");
                    RuleFor(x => x.Sender!.Address.DistrictId)
                        .GreaterThan(0).WithMessage("Mã quận/huyện người gửi không hợp lệ.");
                    RuleFor(x => x.Sender!.Address.WardCode)
                        .NotEmpty().WithMessage("Mã phường/xã người gửi không hợp lệ.");
                }).Otherwise(() =>
                {
                    RuleFor(x => x.Sender).Must(x => false).WithMessage("Thiếu địa chỉ người gửi.");
                });
            });

            When(x => x.Receiver != null, () =>
            {
                RuleFor(x => x.Receiver!.FullName).NotEmpty().WithMessage("Thiếu tên người nhận.");
                RuleFor(x => x.Receiver!.Phone).NotEmpty().WithMessage("Thiếu số điện thoại người nhận.");

                When(x => x.Receiver!.Address != null, () =>
                {
                    RuleFor(x => x.Receiver!.Address.AddressDetail)
                        .NotEmpty().WithMessage("Thiếu địa chỉ chi tiết người nhận.");
                    RuleFor(x => x.Receiver!.Address.DistrictId)
                        .GreaterThan(0).WithMessage("Mã quận/huyện người nhận không hợp lệ.");
                    RuleFor(x => x.Receiver!.Address.WardCode)
                        .NotEmpty().WithMessage("Mã phường/xã người nhận không hợp lệ.");
                }).Otherwise(() =>
                {
                    RuleFor(x => x.Receiver).Must(x => false).WithMessage("Thiếu địa chỉ người nhận.");
                });
            });

            RuleFor(x => x.ServiceTypeId)
                .Must(value => value is LightGoodsServiceTypeId or HeavyGoodsServiceTypeId)
                .WithMessage("Loại dịch vụ GHN chỉ nhận 2 (hàng nhẹ) hoặc 5 (hàng nặng).");

            RuleFor(x => x.RequiredNote)
                .NotEmpty().WithMessage("RequiredNote không được để trống.")
                .Must(note => note is not null && AllowedRequiredNotes.Contains(note.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("RequiredNote chỉ nhận CHOTHUHANG, CHOXEMHANGKHONGTHU hoặc KHONGCHOXEMHANG.");

            RuleFor(x => x.WeightGram)
                .Must(v => v is null or (>= 1 and <= MaxWeightGram))
                .WithMessage($"Khối lượng phải từ 1 đến {MaxWeightGram} gram.");

            ValidateOptionalDimension(x => x.LengthCm, "Chiều dài");
            ValidateOptionalDimension(x => x.WidthCm, "Chiều rộng");
            ValidateOptionalDimension(x => x.HeightCm, "Chiều cao");

            When(x => x.ServiceTypeId == HeavyGoodsServiceTypeId && x.Items.Count > 0, () =>
            {
                RuleForEach(x => x.Items)
                    .SetValidator(new CalculateGhnFeeItemRequestValidator());
            });
        }

        private void ValidateOptionalDimension(
            System.Linq.Expressions.Expression<Func<GhnShippingPreviewRequest, int?>> selector,
            string fieldName)
        {
            RuleFor(selector)
                .Must(v => v is null or (>= 1 and <= MaxDimensionCm))
                .WithMessage($"{fieldName} phải từ 1 đến {MaxDimensionCm} cm.");
        }
    }
}
