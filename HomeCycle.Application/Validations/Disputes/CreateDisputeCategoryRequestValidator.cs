using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class CreateDisputeCategoryRequestValidator : AbstractValidator<CreateDisputeCategoryRequest>
    {
        public CreateDisputeCategoryRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(100)
                .Matches("^[A-Za-z][A-Za-z0-9_]*$")
                .WithMessage("Code chỉ được chứa chữ, số và dấu gạch dưới.");

            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);

            RuleFor(x => x.TargetTypes)
                .NotEmpty()
                .Must(x => x.All(Enum.IsDefined))
                .WithMessage("TargetTypes chứa giá trị không hợp lệ.");
        }
    }
}
