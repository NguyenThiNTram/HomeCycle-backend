using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class CreateProductAttributeRequestValidator : AbstractValidator<CreateAttributeRequest>
    {
        public CreateProductAttributeRequestValidator()
        {
            RuleFor(x => x.AttributeName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Unit)
                .MaximumLength(50);
        }
    }

    public class UpdateProductAttributeRequestValidator : AbstractValidator<UpdateAttributeRequest>
    {
        public UpdateProductAttributeRequestValidator()
        {
            RuleFor(x => x.AttributeName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Unit)
                .MaximumLength(50);
        }
    }
}
