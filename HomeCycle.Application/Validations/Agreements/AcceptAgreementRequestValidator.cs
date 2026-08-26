using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Agreements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Agreements
{
    public sealed class AcceptAgreementRequestValidator : AbstractValidator<AcceptAgreementRequest>
    {
        public AcceptAgreementRequestValidator()
        {
            RuleFor(x => x.ExpectedRevision)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Thiếu thông tin phiên bản thỏa thuận (expectedRevision) để xác nhận.");
        }
    }
}
