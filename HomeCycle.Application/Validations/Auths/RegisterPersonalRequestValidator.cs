using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public class RegisterPersonalRequestValidator : AbstractValidator<RegisterPersonalRequest>
    {
        public RegisterPersonalRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is invalid.")
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(255);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .Matches(@"^0[0-9]{8,9}$").WithMessage("The phone number must have 9 or 10 digits and start with a 0.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}
