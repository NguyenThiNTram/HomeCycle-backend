using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Users
{
    public class UpdateAvatarRequestValidator : AbstractValidator<UpdateAvatarRequest>
    {
        public UpdateAvatarRequestValidator()
        {
            RuleFor(x => x.AvatarUrl)
                .NotEmpty().WithMessage("Đường dẫn ảnh đại diện không được để trống.")
                .MaximumLength(500).WithMessage("Đường dẫn ảnh đại diện quá dài.");
        }
    }
}
