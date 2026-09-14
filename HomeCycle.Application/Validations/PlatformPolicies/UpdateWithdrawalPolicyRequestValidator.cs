using FluentValidation;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdateWithdrawalPolicyRequestValidator : AbstractValidator<UpdateWithdrawalPolicyRequest>
    {
        public UpdateWithdrawalPolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.MinimumWithdrawalAmount.HasValue || x.MaximumWithdrawalAmount.HasValue || x.DailyWithdrawalLimit.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.MinimumWithdrawalAmount)
                .Must(IsValidAmount)
                .WithMessage("MinimumWithdrawalAmount phải là số nguyên dương theo đơn vị VNĐ.");

            RuleFor(x => x.MaximumWithdrawalAmount)
                .Must(IsValidAmount)
                .WithMessage("MaximumWithdrawalAmount phải là số nguyên dương theo đơn vị VNĐ.");

            RuleFor(x => x.DailyWithdrawalLimit)
                .Must(IsValidAmount)
                .WithMessage("DailyWithdrawalLimit phải là số nguyên dương theo đơn vị VNĐ.");
        }

        private static bool IsValidAmount(decimal? value)
            => !value.HasValue || (value.Value > 0 && value.Value == decimal.Truncate(value.Value));
    }
}
