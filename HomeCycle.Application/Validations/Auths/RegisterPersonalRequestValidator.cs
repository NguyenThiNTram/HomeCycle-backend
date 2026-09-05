using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.DTOs.Requests.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public class RegisterPersonalRequestValidator : AbstractValidator<RegisterPersonalRequest>
    {
        public RegisterPersonalRequestValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100)
                    .WithMessage("Full name must not exceed 100 characters.")
                .Must(IsValidFullName)
                    .WithMessage("Full name must contain letters and spaces only.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(100)
                    .WithMessage("Username must not exceed 100 characters.")
                .Must(IsValidUsername)
                    .WithMessage("Username may contain only letters, numbers, and underscores.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6)
                    .WithMessage("Password must be at least 6 characters.")
                .MaximumLength(50)
                    .WithMessage("Password must not exceed 50 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                    .WithMessage("Phone number is required.")
                .Must(IsValidPhoneNumber)
                    .WithMessage("Phone number must contain 10 or 11 digits and start with 0.");
        }
        private static bool IsValidFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            var value = fullName.Trim();

            return Regex.IsMatch(value, @"^[\p{L}]+(?: [\p{L}]+)*$");
        }

        private static bool IsValidUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return Regex.IsMatch(username.Trim(), @"^[a-zA-Z0-9_]+$");
        }

        private static bool IsValidPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // 1. Chuẩn hóa: Xóa khoảng trắng, dấu chấm, dấu gạch ngang (nếu có)
            string cleanNumber = phoneNumber.Replace(" ", "").Replace(".", "").Replace("-", "").Trim();

            // 2. Regex kiểm tra:
            // Nhóm 1 (Di động): Cho phép +84 / 84 / 0 đi kèm với đầu 3, 5, 07, 8, 9 và 8 số cuối
            // Nhóm 2 (Số bàn):  Cho phép +84 / 84 / 0 đi kèm với đầu 2 và 9 số cuối
            string pattern = @"^(?:\+84|84|0)(?:[35789]\d{8}|2\d{9})$";

            return Regex.IsMatch(cleanNumber, pattern);
        }
    }
}
