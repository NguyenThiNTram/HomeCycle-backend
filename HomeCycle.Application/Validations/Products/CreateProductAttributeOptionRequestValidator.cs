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
                .NotEmpty().WithMessage("Giá trị lựa chọn không được để trống.")
                .MaximumLength(255).WithMessage("Giá trị lựa chọn không được vượt quá 255 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue)
                .WithMessage("Thứ tự hiển thị không được âm.");
        }
    }

    public class UpdateProductAttributeOptionRequestValidator : AbstractValidator<UpdateAttributeOptionRequest>
    {
        public UpdateProductAttributeOptionRequestValidator()
        {
            RuleFor(x => x.OptionValue)
               .NotEmpty().WithMessage("Giá trị lựa chọn không được để trống.")
               .MaximumLength(255).WithMessage("Giá trị lựa chọn không được vượt quá 255 ký tự.");
        }
    }
}
