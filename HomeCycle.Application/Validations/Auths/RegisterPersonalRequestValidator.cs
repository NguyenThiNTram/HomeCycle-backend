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
                .MaximumLength(20)
                    .WithMessage("Password must not exceed 20 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                    .WithMessage("Phone number is required.")
                .Must(IsValidPhoneNumber)
                    .WithMessage("Phone number must contain 9 or 10 digits and start with 0.");
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
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return Regex.IsMatch(phoneNumber.Trim(), @"^0\d{8,9}$");
        }
    }
}
