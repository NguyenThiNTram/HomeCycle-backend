using FluentValidation;
using HomeCycle.Application.DTOs.Requests.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.GHN
{
    public sealed class CalculateGhnFeeRequestValidator : AbstractValidator<CalculateGhnFeeRequest>
    {
        private const int LightGoodsServiceTypeId = 2;
        private const int HeavyGoodsServiceTypeId = 5;

        private const int MaxWeightGram = 1_600_000;
        private const int MaxDimensionCm = 200;

        public CalculateGhnFeeRequestValidator()
        {
            RuleFor(x => x.FromDistrictId)
                .GreaterThan(0)
                .WithMessage("Mã quận/huyện người gửi không hợp lệ.");

            RuleFor(x => x.FromWardCode)
                .NotEmpty()
                .WithMessage("Mã phường/xã người gửi không được để trống.");

            RuleFor(x => x.ToDistrictId)
                .GreaterThan(0)
                .WithMessage("Mã quận/huyện người nhận không hợp lệ.");

            RuleFor(x => x.ToWardCode)
                .NotEmpty()
                .WithMessage("Mã phường/xã người nhận không được để trống.");

            RuleFor(x => x.ServiceTypeId)
                .Must(value =>
                    value is LightGoodsServiceTypeId or HeavyGoodsServiceTypeId)
                .WithMessage(
                    "Loại dịch vụ GHN chỉ nhận 2 (hàng nhẹ) hoặc 5 (hàng nặng).");

            RuleFor(x => x.WeightGram)
                .InclusiveBetween(1, MaxWeightGram)
                .WithMessage(
                    $"Khối lượng phải từ 1 đến {MaxWeightGram} gram.");

            When(
                x => x.ServiceTypeId == LightGoodsServiceTypeId,
                () =>
                {
                    ValidateLightGoodsDimension(
                        x => x.LengthCm,
                        "Chiều dài");

                    ValidateLightGoodsDimension(
                        x => x.WidthCm,
                        "Chiều rộng");

                    ValidateLightGoodsDimension(
                        x => x.HeightCm,
                        "Chiều cao");
                });

            When(
                x => x.ServiceTypeId == HeavyGoodsServiceTypeId,
                () =>
                {
                    RuleFor(x => x.Items)
                        .NotNull()
                        .WithMessage("Danh sách kiện hàng không được để null.")
                        .Must(items => items is { Count: > 0 })
                        .WithMessage(
                            "Hàng nặng phải có ít nhất một kiện hàng.");

                    RuleForEach(x => x.Items)
                        .SetValidator(
                            new CalculateGhnFeeItemRequestValidator());
                }
            );
        }

    private void ValidateLightGoodsDimension(System.Linq.Expressions.Expression<Func<CalculateGhnFeeRequest, int?>> selector,
        string fieldName)
        {
            RuleFor(selector)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage($"{fieldName} là bắt buộc đối với hàng nhẹ.")
                .Must(value => value is >= 1 and <= MaxDimensionCm)
                .WithMessage(
                    $"{fieldName} phải từ 1 đến {MaxDimensionCm} cm.");
        }
    }
}

