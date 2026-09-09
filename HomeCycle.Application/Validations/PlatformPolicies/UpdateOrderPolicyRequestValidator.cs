using FluentValidation;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdateOrderPolicyRequestValidator : AbstractValidator<UpdateOrderPolicyRequest>
    {
        public UpdateOrderPolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.BuyerReceiveConfirmationTimeoutHours.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.BuyerReceiveConfirmationTimeoutHours)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 720)
                .WithMessage("BuyerReceiveConfirmationTimeoutHours phải từ 1 đến 720 giờ.");
        }
    }
}
