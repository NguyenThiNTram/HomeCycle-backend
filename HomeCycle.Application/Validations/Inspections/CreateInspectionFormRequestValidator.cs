using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Inspections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Inspections
{
    public class CreateInspectionFormRequestValidator : AbstractValidator<CreateInspectionFormRequest>
    {
        public CreateInspectionFormRequestValidator()
        {
            RuleFor(x => x.InspectorNotes).MaximumLength(2000);

            RuleFor(x => x.SuggestedPrice)
                .GreaterThan(0)
                .When(x => x.SuggestedPrice.HasValue);

            RuleFor(x => x.Images)
                .Must(x => x == null || x.Count <= 5)
                .WithMessage("Biểu mẫu kiểm định chỉ cho phép tối đa 5 hình ảnh.");
        }
    }
}
