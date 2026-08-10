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
        private const int MaxWeightGram = 1_600_000;
        private const int MaxDimensionCm = 200;

        public CalculateGhnFeeItemRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên kiện hàng không được để trống.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Số lượng kiện hàng phải lớn hơn 0.");

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
