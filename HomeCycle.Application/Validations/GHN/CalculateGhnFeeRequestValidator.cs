using FluentValidation;
using HomeCycle.Application.DTOs.Requests.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.GHN
{
    public sealed class CalculateGhnFeeRequestValidator
    : AbstractValidator<CalculateGhnFeeRequest>
    {
        public CalculateGhnFeeRequestValidator()
        {
            RuleFor(x => x.ServiceTypeId)
                .Must(x => x is 2 or 5)
                .WithMessage("ServiceTypeId chỉ chấp nhận 2 hoặc 5.");

            RuleFor(x => x.WeightGram)
                .GreaterThan(0)
                .LessThanOrEqualTo(1_600_000);

            When(x => x.ServiceTypeId == 2, () =>
            {
                RuleFor(x => x.LengthCm).NotNull().InclusiveBetween(1, 200);
                RuleFor(x => x.WidthCm).NotNull().InclusiveBetween(1, 200);
                RuleFor(x => x.HeightCm).NotNull().InclusiveBetween(1, 200);
            });

            When(x => x.ServiceTypeId == 5, () =>
            {
                RuleFor(x => x.Items).NotEmpty();

                RuleForEach(x => x.Items).ChildRules(item =>
                {
                    item.RuleFor(x => x.Quantity).GreaterThan(0);
                    item.RuleFor(x => x.WeightGram).GreaterThan(0);
                    item.RuleFor(x => x.LengthCm).InclusiveBetween(1, 200);
                    item.RuleFor(x => x.WidthCm).InclusiveBetween(1, 200);
                    item.RuleFor(x => x.HeightCm).InclusiveBetween(1, 200);
                });
            });
        }
    }
}
