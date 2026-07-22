using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public class RegisterBusinessAccountRequestValidator : AbstractValidator<RegisterBusinessAccountRequest>
    {
        public RegisterBusinessAccountRequestValidator()
        {

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(255);
        }
    }
}
