using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Agreements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Agreements
{
    public class UpdateAgreementFormRequestValidator : AbstractValidator<UpdateAgreementFormRequest>
    {
        public UpdateAgreementFormRequestValidator()
        {
            RuleFor(x => x.AgreementType).IsInEnum().WithMessage("Invalid agreement type.");
            RuleFor(x => x.PaymentType).IsInEnum().WithMessage("Invalid payment type.");
            RuleFor(x => x.AgreementDetails).NotNull().WithMessage("Agreement details cannot be empty.");

            // FIX TƯƠNG TỰ
            When(x => x.AgreementDetails != null, () =>
            {
                RuleFor(x => x.AgreementDetails)
                    .SetValidator(req => new AgreementDetailsDtoValidator(req.AgreementType));
            });
        }
    }
}
