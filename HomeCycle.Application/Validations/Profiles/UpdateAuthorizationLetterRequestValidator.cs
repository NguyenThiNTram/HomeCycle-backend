using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.Validations.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateAuthorizationLetterRequestValidator : AbstractValidator<UpdateAuthorizationLetterRequest>
    {
        public UpdateAuthorizationLetterRequestValidator()
        {
            RuleFor(x => x.AuthorizationLetter)
                .NotNull().WithMessage("Vui lòng đính kèm Giấy ủy quyền.");

            RuleFor(x => x.AuthorizationLetter).SetValidator(new FormFileValidator());
        }
    }
}
