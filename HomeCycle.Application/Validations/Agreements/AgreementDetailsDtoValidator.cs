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

            // Case 2b: Giao hàng qua GHN -> bắt buộc có thông tin vận chuyển GHN
            When(x => x.DeliveryMethod == DeliveryMethod.GhnDelivery, () =>
            {
                RuleFor(x => x.GhnInfo)
                    .NotNull().WithMessage("GHN shipping info is required for GHN delivery.");

                When(x => x.GhnInfo != null, () =>
                {
                    RuleFor(x => x.GhnInfo!.Sender).NotNull().WithMessage("GHN sender info is required.");
                    RuleFor(x => x.GhnInfo!.Receiver).NotNull().WithMessage("GHN receiver info is required.");

                    RuleFor(x => x.GhnInfo!.PaymentTypeId)
                        .Must(v => v is null)
                        .WithMessage("PaymentTypeId do hệ thống tự khóa theo chính sách, không nhận từ Client.");

                    // Quote & QuoteStatus: là KẾT QUẢ Backend tự gọi GHN CalculateFee rồi gán vào
                    // không phải input — Từ chối thẳng nếu Client cố gửi giá trị khác null
                    RuleFor(x => x.GhnInfo!.Quote)
                        .Must(q => q is null)
                        .WithMessage("Quote do hệ thống tự tính sau khi gọi GHN, không nhận từ Client.");

                    RuleFor(x => x.GhnInfo!.QuoteStatus)
                        .Must(s => s is null)
                        .WithMessage("QuoteStatus do hệ thống tự cập nhật, không nhận từ Client.");
                });
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
