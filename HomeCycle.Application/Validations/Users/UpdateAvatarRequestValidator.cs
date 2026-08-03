using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Users;
using Microsoft.AspNetCore.Http;
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
                .NotEmpty().WithMessage("The avatar field cannot be left blank.")
                .Must(BeAValidUrl).WithMessage("Invalid avatar URL");
        }

        private bool BeAValidUrl(IFormFile file)
        {
            return Uri.TryCreate(file.FileName, UriKind.Absolute, out _);
        }
    }
}
