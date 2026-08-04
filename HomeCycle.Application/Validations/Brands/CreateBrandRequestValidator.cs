using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Brands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Brands
{
    public class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
    {
        public CreateBrandRequestValidator()
        {
            RuleFor(x => x.BrandName)
                .NotEmpty()
                .WithMessage("Brand name is required.")
                .MaximumLength(100)
                .WithMessage("Brand name cannot exceed 100 characters.");


            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
