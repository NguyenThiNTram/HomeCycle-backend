using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class ProductRequestValidator : AbstractValidator<ProductRequest>
    {
        public ProductRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty();

            RuleFor(x => x.ProductTypeId)
                .NotEmpty();

            RuleFor(x => x.ProductName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.ModelNumber)
                .MaximumLength(100)
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Any(char.IsLetterOrDigit))
                .WithMessage("Mã model tối đa 100 ký tự và phải chứa ít nhất một chữ cái hoặc chữ số.");

            RuleFor(x => x.FunctionalityStatus)
                .IsInEnum()
                .When(x => x.FunctionalityStatus.HasValue)
                .WithMessage("Tình trạng hoạt động không hợp lệ.");

            RuleFor(x => x.DamageLevel)
                .IsInEnum()
                .When(x => x.DamageLevel.HasValue)
                .WithMessage("Mức độ hư hại không hợp lệ.");

            RuleFor(x => x.OriginalPrice)
                .GreaterThanOrEqualTo(0).When(x => x.OriginalPrice.HasValue);

            RuleFor(x => x.Length)
                .GreaterThan(0).When(x => x.Length.HasValue);
            RuleFor(x => x.Width)
                .GreaterThan(0).When(x => x.Width.HasValue);
            RuleFor(x => x.Height)
                .GreaterThan(0).When(x => x.Height.HasValue);
            RuleFor(x => x.Weight)
                .GreaterThan(0).When(x => x.Weight.HasValue);

            RuleForEach(x => x.AttributeValues)
                .SetValidator(new ProductAttributeValueRequestValidator());
        }
    }
}
