using FluentValidation;
using HomeCycle.Application.DTOs.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class FileUploadPolicyConfigDtoValidator : AbstractValidator<FileUploadPolicyConfigDto>
    {
        public FileUploadPolicyConfigDtoValidator(IValidator<FileUploadRuleConfigDto> ruleValidator)
        {
            RuleFor(x => x.Rules)
                .NotNull()
                .WithMessage("Danh sách file upload rule không được null.")
                .NotEmpty()
                .WithMessage("File upload policy phải chứa ít nhất một rule.")
                .Must(HaveUniqueContexts)
                .WithMessage("Mỗi ngữ cảnh upload chỉ được xuất hiện một lần.");

            RuleForEach(x => x.Rules).SetValidator(ruleValidator);
        }

        private static bool HaveUniqueContexts(IEnumerable<FileUploadRuleConfigDto>? rules)
        {
            if (rules == null)
                return false;

            var contexts = rules.Select(x => x.Context).ToList();
            return contexts.Count == contexts.Distinct().Count();
        }

    }

}
