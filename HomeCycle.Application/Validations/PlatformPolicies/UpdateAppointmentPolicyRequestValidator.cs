using FluentValidation;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdateAppointmentPolicyRequestValidator : AbstractValidator<UpdateAppointmentPolicyRequest>
    {
        public UpdateAppointmentPolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    x.CheckInOpenBeforeMinutes.HasValue ||
                    x.NoInteractionExpiryMinutes.HasValue ||
                    x.RescheduleCutoffHours.HasValue ||
                    x.CancellationCutoffHours.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.CheckInOpenBeforeMinutes)
                .Must(x => !x.HasValue || x.Value is >= 0 and <= 1440)
                .WithMessage("CheckInOpenBeforeMinutes phải từ 0 đến 1440 phút.");

            RuleFor(x => x.NoInteractionExpiryMinutes)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 10080)
                .WithMessage("NoInteractionExpiryMinutes phải từ 1 đến 10080 phút.");

            RuleFor(x => x.RescheduleCutoffHours)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 720)
                .WithMessage("RescheduleCutoffHours phải từ 1 đến 720 giờ.");

            RuleFor(x => x.CancellationCutoffHours)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 720)
                .WithMessage("CancellationCutoffHours phải từ 1 đến 720 giờ.");
        }
    }
}
