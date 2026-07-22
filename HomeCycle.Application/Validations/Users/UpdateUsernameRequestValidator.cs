using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Users
{
    public class UpdateUsernameRequestValidator : AbstractValidator<UpdateUsernameRequest>
    {
        public  UpdateUsernameRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username không được để trống.")
                .MinimumLength(3).WithMessage("Username phải có ít nhất 3 ký tự.")
                .MaximumLength(50).WithMessage("Username không được vượt quá 50 ký tự.")
                .Matches("^[a-zA-Z0-9_.]+$").WithMessage("Username chỉ được chứa chữ cái, số, dấu gạch dưới và dấu chấm.");
        }
    }
}
