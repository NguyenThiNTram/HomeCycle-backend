using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Inspections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Inspections
{
    public class InspectionRevisionRequestValidator : AbstractValidator<InspectionRevisionRequest>
    {
        public InspectionRevisionRequestValidator()
        {
            RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1);
        }
    }

}
