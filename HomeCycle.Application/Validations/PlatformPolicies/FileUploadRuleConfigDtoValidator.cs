using FluentValidation;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class FileUploadRuleConfigDtoValidator : AbstractValidator<FileUploadRuleConfigDto>
    {
        public FileUploadRuleConfigDtoValidator()
        {
            RuleFor(x => x.Context)
                .Must(context => Enum.IsDefined(typeof(FileUploadContext), context))
                .WithMessage("Ngữ cảnh upload không được hỗ trợ.");

            RuleFor(x => x.MaxFileSizeBytes)
                .GreaterThan(0)
                .WithMessage("MaxFileSizeBytes phải lớn hơn 0.")
                .LessThanOrEqualTo(FileUploadPolicyConstraints.MaxConfigurableFileSizeBytes)
                .WithMessage(
                    $"MaxFileSizeBytes không được vượt quá " +
                    $"{FileUploadPolicyConstraints.MaxConfigurableFileSizeBytes} bytes.");

            RuleFor(x => x.AllowedExtensions)
                .NotNull()
                .WithMessage("Danh sách phần mở rộng được phép không được null.")
                .NotEmpty()
                .WithMessage("Phải cấu hình ít nhất một phần mở rộng được phép.")
                .Must(ContainOnlySupportedExtensions)
                .WithMessage("Danh sách chứa phần mở rộng không được hệ thống hỗ trợ.")
                .Must(HaveUniqueExtensions)
                .WithMessage("Danh sách phần mở rộng không được chứa giá trị trùng nhau.");
        }

        private static bool ContainOnlySupportedExtensions(
            IEnumerable<string>? extensions)
        {
            return extensions != null &&
                   extensions.All(FileTypeCatalog.IsSupportedExtension);
        }

        private static bool HaveUniqueExtensions(
            IEnumerable<string>? extensions)
        {
            if (extensions == null)
                return false;

            var values = extensions
                .Select(FileTypeCatalog.NormalizeExtension)
                .ToList();

            return values.Count ==
                   values.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }
    }
}
