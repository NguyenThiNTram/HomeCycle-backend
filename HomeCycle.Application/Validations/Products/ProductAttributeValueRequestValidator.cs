using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class ProductAttributeValueRequestValidator : AbstractValidator<ProductAttributeValueRequest>
    {
        public ProductAttributeValueRequestValidator()
        {
            RuleFor(x => x.AttributeId)
                .NotEmpty()
                .WithMessage("Attribute is required.");

            When(x => x.ValueText != null, () =>
            {
                RuleFor(x => x.ValueText)
                    .MaximumLength(1000)
                    .WithMessage("Text value cannot exceed 1000 characters.");
            });

            When(x => x.ValueNumber.HasValue, () =>
            {
                RuleFor(x => x.ValueNumber!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Numeric value must be greater than or equal to 0.");
            });

            RuleFor(x => x).Custom((request, context) =>
            {
                if (!HaveOnlyOneValue(request))
                {
                    context.AddFailure($"Thuộc tính '{request.AttributeId}' chỉ được phép điền 1 trong 4 trường: OptionId, ValueText, ValueNumber, ValueBoolean.");
                }
            });
        }

        private static bool HaveOnlyOneValue(ProductAttributeValueRequest request)
        {
            var count = 0;

            if (request.OptionId.HasValue)
                count++;

            if (!string.IsNullOrWhiteSpace(request.ValueText))
                count++;

            if (request.ValueNumber.HasValue)
                count++;

            if (request.ValueBoolean.HasValue)
                count++;

            return count <= 1;
        }
    }
}
