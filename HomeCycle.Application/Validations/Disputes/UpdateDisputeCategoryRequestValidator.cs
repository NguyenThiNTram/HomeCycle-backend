using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class UpdateDisputeCategoryRequestValidator : AbstractValidator<UpdateDisputeCategoryRequest>
    {
        public UpdateDisputeCategoryRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Name != null || x.Description != null || x.TargetTypes != null)
                .WithMessage("Phải cung cấp ít nhất một nội dung cần cập nhật.");

            RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name != null);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);

            RuleFor(x => x.TargetTypes)
                .NotEmpty()
                .Must(x => x != null && x.All(Enum.IsDefined))
                .When(x => x.TargetTypes != null)
                .WithMessage("TargetTypes chứa giá trị không hợp lệ.");
        }
    }
}
