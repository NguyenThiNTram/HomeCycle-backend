using FluentValidation;
using HomeCycle.Application.DTOs.Requests.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.GHN
{
    public sealed class CalculateGhnFeeItemRequestValidator : AbstractValidator<CalculateGhnFeeItemRequest>
    {
        private const int MaxWeightGram = HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.MaxShipmentWeightGram;
        private const int MaxDimensionCm = 200;

        public CalculateGhnFeeItemRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên kiện hàng không được để trống.")
                .MaximumLength(512);

            RuleFor(x => x.Quantity)
                .Equal(1)
                .WithMessage("Mỗi item là một kiện vật lý theo quy ước HomeCycle; Quantity phải bằng 1.");

            RuleFor(x => x.WeightGram)
                .InclusiveBetween(1, MaxWeightGram)
                .WithMessage(
                    $"Khối lượng kiện hàng phải từ 1 đến {MaxWeightGram} gram.");

            RuleFor(x => x.LengthCm)
                .InclusiveBetween(1, MaxDimensionCm)
                .WithMessage(
                    $"Chiều dài kiện hàng phải từ 1 đến {MaxDimensionCm} cm.");

            RuleFor(x => x.WidthCm)
                .InclusiveBetween(1, MaxDimensionCm)
                .WithMessage(
                    $"Chiều rộng kiện hàng phải từ 1 đến {MaxDimensionCm} cm.");

            RuleFor(x => x.HeightCm)
                .InclusiveBetween(1, MaxDimensionCm)
                .WithMessage(
                    $"Chiều cao kiện hàng phải từ 1 đến {MaxDimensionCm} cm.");
        }
    }
}
