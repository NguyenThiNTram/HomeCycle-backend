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
    public class UpdateDisputePolicyRequestValidator : AbstractValidator<UpdateDisputePolicyRequest>
    {
        public UpdateDisputePolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    x.NormalDisputeWindowDays.HasValue ||
                    x.LowReputationDisputeWindowDays.HasValue ||
                    x.LowReputationThreshold.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.NormalDisputeWindowDays)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 365)
                .WithMessage("NormalDisputeWindowDays phải từ 1 đến 365 ngày.");

            RuleFor(x => x.LowReputationDisputeWindowDays)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 365)
                .WithMessage("LowReputationDisputeWindowDays phải từ 1 đến 365 ngày.");

            RuleFor(x => x.LowReputationThreshold)
                .Must(x => !x.HasValue || x.Value is >= 0 and <= 100)
                .WithMessage("LowReputationThreshold phải từ 0 đến 100.");
        }
    }
}
