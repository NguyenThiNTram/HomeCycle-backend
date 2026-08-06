using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Agreements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Agreements
{
    public class CreateAgreementFormRequestValidator : AbstractValidator<CreateAgreementFormRequest>
    {
        public CreateAgreementFormRequestValidator()
        {
            RuleFor(x => x.NegotiationId)
                .NotEmpty().WithMessage("Negotiation ID is invalid or empty.");

            RuleFor(x => x.AgreementType)
                .IsInEnum().WithMessage("Invalid agreement type.");

            RuleFor(x => x.PaymentType)
                .IsInEnum().WithMessage("Invalid payment type.");

            RuleFor(x => x.AgreementDetails)
                .NotNull().WithMessage("Agreement details cannot be empty.");

            // FIX LỖI Ở ĐÂY: Sử dụng overload SetValidator hỗ trợ truyền object cha (req)
            When(x => x.AgreementDetails != null, () =>
            {
                RuleFor(x => x.AgreementDetails)
                    .SetValidator(req => new AgreementDetailsDtoValidator(req.AgreementType));
            });
        }
    }
}
