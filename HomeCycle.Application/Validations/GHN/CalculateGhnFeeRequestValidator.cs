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
        //private const int LightGoodsServiceTypeId = 2;
        //private const int HeavyGoodsServiceTypeId = 5;

        //private const int MaxWeightGram = 1_600_000;
        //private const int MaxDimensionCm = 200;

        public CalculateGhnFeeRequestValidator()
        {
            RuleFor(x => x.ParcelCount).GreaterThan(0);
            RuleFor(x => x.ServiceTypeId)
                .Must((request, type) => type == (request.WeightGram >= 20_000 || request.ParcelCount > 1 ? 5 : 2))
                .WithMessage("Loại dịch vụ phải khớp tổng khối lượng và số kiện.");
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
                //.Must(value =>
                //    value is LightGoodsServiceTypeId or HeavyGoodsServiceTypeId)
                //.WithMessage(
                //    "Loại dịch vụ GHN chỉ nhận 2 (hàng nhẹ) hoặc 5 (hàng nặng).");
                .Must(x => x is 2 or 5)
                .WithMessage(
                    "ServiceTypeId chỉ nhận 2 (<20kg) hoặc 5 (>=20kg hoặc nhiều kiện).");

            RuleFor(x => x.WeightGram)
                //.InclusiveBetween(1, MaxWeightGram)
                //.WithMessage(
                //    $"Khối lượng phải từ 1 đến {MaxWeightGram} gram.");
                .GreaterThan(0)
                .WithMessage("Khối lượng kiện hàng phải lớn hơn 0 gram.");

            RuleFor(x => x.LengthCm)
                .Must(x => x is null || x > 0)
                .WithMessage("LengthCm phải lớn hơn 0 nếu được cung cấp.");

            RuleFor(x => x.WidthCm)
                .Must(x => x is null || x > 0)
                .WithMessage("WidthCm phải lớn hơn 0 nếu được cung cấp.");

            RuleFor(x => x.HeightCm)
                .Must(x => x is null || x > 0)
                .WithMessage("HeightCm phải lớn hơn 0 nếu được cung cấp.");

            // Contract mới xác định type 2 là dưới 20kg.
            When(x => x.ServiceTypeId == 2, () =>
            {
                RuleFor(x => x.WeightGram)
                    .LessThan(20_000)
                    .WithMessage(
                        "ServiceTypeId = 2 chỉ dùng khi tổng khối lượng dưới 20kg.");
            });

            RuleForEach(x => x.Items)
                .SetValidator(new CalculateGhnFeeItemRequestValidator());

            //When(
            //    x => x.ServiceTypeId == LightGoodsServiceTypeId,
            //    () =>
            //    {
            //        ValidateLightGoodsDimension(
            //            x => x.LengthCm,
            //            "Chiều dài");

            //        ValidateLightGoodsDimension(
            //            x => x.WidthCm,
            //            "Chiều rộng");

            //        ValidateLightGoodsDimension(
            //            x => x.HeightCm,
            //            "Chiều cao");
            //    });

            //When(
            //    x => x.ServiceTypeId == HeavyGoodsServiceTypeId,
            //    () =>
            //    {
            //        RuleFor(x => x.Items)
            //            .NotNull()
            //            .WithMessage("Danh sách kiện hàng không được để null.")
            //            .Must(items => items is { Count: > 0 })
            //            .WithMessage(
            //                "Hàng nặng phải có ít nhất một kiện hàng.");

            //        RuleForEach(x => x.Items)
            //            .SetValidator(
            //                new CalculateGhnFeeItemRequestValidator());
            //    }
            //);
        }

        //private void ValidateLightGoodsDimension(System.Linq.Expressions.Expression<Func<CalculateGhnFeeRequest, int?>> selector,
        //    string fieldName)
        //    {
        //        RuleFor(selector)
        //            .Cascade(CascadeMode.Stop)
        //            .NotNull()
        //            .WithMessage($"{fieldName} là bắt buộc đối với hàng nhẹ.")
        //            .Must(value => value is >= 1 and <= MaxDimensionCm)
        //            .WithMessage(
        //                $"{fieldName} phải từ 1 đến {MaxDimensionCm} cm.");
        //    }
        //}
    }
}

