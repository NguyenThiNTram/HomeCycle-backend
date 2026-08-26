using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.DTOs.Requests.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100)
                .WithMessage("Email must not exceed 100 characters.")
            .EmailAddress()
                .WithMessage("Email format is invalid.");

            RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("Password is required.")
            .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters.")
            .MaximumLength(20)
                .WithMessage("Password must not exceed 20 characters.");
        }
    }
}
