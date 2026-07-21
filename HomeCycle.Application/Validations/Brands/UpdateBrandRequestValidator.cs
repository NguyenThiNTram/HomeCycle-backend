using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Brands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Brands
{
    public class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
    {
        public UpdateBrandRequestValidator()
        {
            RuleFor(x => x.BrandName)
                .NotEmpty()
                .WithMessage("Brand name is required.")
                .MaximumLength(100);


            RuleFor(x => x.Description)
                .MaximumLength(500);
        }
    }
}
