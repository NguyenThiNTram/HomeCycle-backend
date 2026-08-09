using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.Validations.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateIdentityRequestValidator : AbstractValidator<UpdateIdentityRequest>
    {
        public UpdateIdentityRequestValidator()
        {
            // --- Text Rules ---
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("FullName is required.")
                .MaximumLength(255).WithMessage("FullName must not exceed 255 characters.")
                .Must((request, fullName) => IsNameMatch(fullName, request.IdentityName))
                .WithMessage("FullName bắt buộc phải khớp chính xác với IdentityName (Tên trên CCCD).");

            RuleFor(x => x.IdentityNumber)
                .NotEmpty().WithMessage("Identity card number is required.")
                .Matches(@"^[0-9]{12}$").WithMessage("CCCD phải đúng 12 số.");

            RuleFor(x => x.IdentityName).NotEmpty().MaximumLength(255);

            RuleFor(x => x.IdentityDob)
                .NotEmpty()
                .Must(dob => dob != default(DateOnly)).WithMessage("Ngày sinh không hợp lệ.")
                .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Ngày sinh không được lớn hơn ngày hiện tại.");


            RuleFor(x => x.IdentityAddress).NotEmpty();

            // --- File Rules ---
            // Nếu có gửi file lên, thì file đó phải hợp lệ (dùng validator file có sẵn trong thư mục Files)
            RuleFor(x => x.CccdFront)
                 .NotNull().WithMessage("Ảnh mặt trước CCCD là bắt buộc mỗi lần cập nhật định danh.")
                 .SetValidator(new FormFileValidator());

            RuleFor(x => x.CccdBack)
                .NotNull().WithMessage("Ảnh mặt sau CCCD là bắt buộc mỗi lần cập nhật định danh.")
                .SetValidator(new FormFileValidator());
        }

        private bool IsNameMatch(string? name1, string? name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2)) return false;
            return NormalizeString(name1) == NormalizeString(name2);
        }

        private string NormalizeString(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim();
            return Regex.Replace(normalized, @"\s+", " ");
        }
    }
}
