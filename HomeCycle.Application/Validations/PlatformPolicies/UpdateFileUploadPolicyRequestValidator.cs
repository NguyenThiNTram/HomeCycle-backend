using FluentValidation;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdateFileUploadPolicyRequestValidator
        : AbstractValidator<UpdateFileUploadPolicyRequest>
    {
        public UpdateFileUploadPolicyRequestValidator()
        {
            RuleFor(x => x.Context)
                .Must(context => Enum.IsDefined(typeof(FileUploadContext), context))
                .WithMessage("Ngữ cảnh upload không được hỗ trợ.");

            RuleFor(x => x)
                .Must(x => x.MaxFileSizeBytes.HasValue ||
                           x.AllowedExtensions != null)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.MaxFileSizeBytes)
                .GreaterThan(0)
                .WithMessage("MaxFileSizeBytes phải lớn hơn 0.")
                .LessThanOrEqualTo(FileUploadPolicyConstraints.MaxConfigurableFileSizeBytes)
                .WithMessage(
                    $"MaxFileSizeBytes không được vượt quá " +
                    $"{FileUploadPolicyConstraints.MaxConfigurableFileSizeBytes} bytes.")
                .When(x => x.MaxFileSizeBytes.HasValue);

            RuleFor(x => x.AllowedExtensions)
                .NotEmpty()
                .WithMessage("AllowedExtensions không được rỗng khi được cung cấp.")
                .Must(ContainOnlySupportedExtensions)
                .WithMessage("Danh sách chứa phần mở rộng không được hệ thống hỗ trợ.")
                .Must(HaveUniqueExtensions)
                .WithMessage("Danh sách phần mở rộng không được chứa giá trị trùng nhau.")
                .When(x => x.AllowedExtensions != null);
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
