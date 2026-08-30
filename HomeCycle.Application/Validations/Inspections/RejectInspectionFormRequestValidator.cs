using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Inspections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Inspections
{
    public class RejectInspectionFormRequestValidator : AbstractValidator<RejectInspectionFormRequest>
    {
        public RejectInspectionFormRequestValidator()
        {
            RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        }
    }
}
