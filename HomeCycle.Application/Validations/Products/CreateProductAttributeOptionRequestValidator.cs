using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class CreateProductAttributeOptionRequestValidator : AbstractValidator<CreateAttributeOptionRequest>
    {
        public CreateProductAttributeOptionRequestValidator()
        {
            RuleFor(x => x.OptionValue)
                .NotEmpty()
                .MaximumLength(255);
        }
    }

    public class UpdateProductAttributeOptionRequestValidator : AbstractValidator<UpdateAttributeOptionRequest>
    {
        public UpdateProductAttributeOptionRequestValidator()
        {
            RuleFor(x => x.OptionValue)
                .NotEmpty()
                .MaximumLength(255);
        }
    }
}
