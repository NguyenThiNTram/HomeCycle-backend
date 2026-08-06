using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Agreements
{
    public class AgreementDetailsDtoValidator : AbstractValidator<AgreementDetailsDto>
    {
        public AgreementDetailsDtoValidator(AgreementType agreementType)
        {
            // Case 1: Không kiểm định (Thu gom) -> Bắt buộc phải có DeliveryMethod
            When(x => agreementType == AgreementType.No_Inspection, () =>
            {
                RuleFor(x => x.DeliveryMethod)
                    .NotNull().WithMessage("Delivery method is required for No_Inspection.")
                    .IsInEnum().WithMessage("Invalid delivery method specified.");
            });

            // Case 2: Validation địa chỉ nếu có DeliveryMethod
            When(x => x.DeliveryMethod.HasValue, () =>
            {
                RuleFor(x => x.PickupAddress)
                    .NotEmpty().WithMessage("Pickup address is required for delivery.");

                RuleFor(x => x.DeliveryAddress)
                    .NotEmpty().WithMessage("Delivery address is required for delivery.");
            });

            // Case 3: Có kiểm định
            When(x => agreementType == AgreementType.Inspection, () =>
            {
                RuleFor(x => x.InspectionAddress)
                    .NotEmpty().WithMessage("Inspection address is required for this agreement type.");

                RuleFor(x => x.InspectionDate)
                    .NotNull().WithMessage("Inspection date is required.")
                    .GreaterThan(DateTime.UtcNow).WithMessage("Inspection date must be in the future.");
            });
        }
    }
}
