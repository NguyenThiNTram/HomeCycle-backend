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
