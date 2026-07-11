using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public sealed class LoginPersonalRequestValidator : AbstractValidator<LoginPersonalRequest>
    {
        public LoginPersonalRequestValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
