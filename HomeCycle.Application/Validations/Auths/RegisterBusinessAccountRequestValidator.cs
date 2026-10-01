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
    public class RegisterBusinessAccountRequestValidator : AbstractValidator<RegisterBusinessAccountRequest>
    {
        public RegisterBusinessAccountRequestValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

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

            string cleanNumber = phoneNumber.Replace(" ", "").Replace(".", "").Replace("-", "").Trim();
            string pattern = @"^(?:\+84|84|0)(?:[35789]\d{8}|2\d{9})$";

            return Regex.IsMatch(cleanNumber, pattern);
        }
    }
}
