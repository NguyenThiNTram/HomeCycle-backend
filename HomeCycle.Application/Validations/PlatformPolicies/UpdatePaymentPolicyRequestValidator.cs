using FluentValidation;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdatePaymentPolicyRequestValidator : AbstractValidator<UpdatePaymentPolicyRequest>
    {
        public UpdatePaymentPolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    x.DepositRatePercent.HasValue ||
                    x.PaymentExpiryMinutes.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.DepositRatePercent)
                .Must(x => !x.HasValue || (x.Value > 0 && x.Value <= 100))
                .WithMessage("DepositRatePercent phải lớn hơn 0 và không vượt quá 100%.");

            RuleFor(x => x.PaymentExpiryMinutes)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 1440)
                .WithMessage("PaymentExpiryMinutes phải từ 1 đến 1440 phút.");
        }
    }
}
