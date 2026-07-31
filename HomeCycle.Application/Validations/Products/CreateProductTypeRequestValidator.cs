using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class CreateProductTypeRequestValidator : AbstractValidator<CreateProductTypeRequest>
    {
        public CreateProductTypeRequestValidator(IValidator<CreateAttributeRequest> attributeValidator)
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty();

            RuleFor(x => x.ProductTypeName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Description)
                .MaximumLength(1000);

            RuleForEach(x => x.Attributes)
                .SetValidator(new CreateProductAttributeRequestValidator());
        }
    }

    public class UpdateProductTypeRequestValidator : AbstractValidator<UpdateProductTypeRequest>
    {
        public UpdateProductTypeRequestValidator()
        {
            RuleFor(x => x.ProductTypeName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}
