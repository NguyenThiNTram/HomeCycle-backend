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
        private static readonly string[] AllowedExtensions =
          [
              ".jpg", ".jpeg", ".png", ".webp"
          ];

        private static readonly string[] AllowedContentTypes =
        [
            "image/jpeg", "image/png", "image/webp"
        ];

        public UpdateAvatarRequestValidator()
        {
            //RuleFor(x => x.AvatarUrl)
            //    .NotEmpty().WithMessage("The avatar field cannot be left blank.")
            //    .Must(BeAValidUrl).WithMessage("Invalid avatar URL");

            RuleFor(x => x.AvatarUrl)
               .Cascade(CascadeMode.Stop)
               .NotNull()
               .WithMessage("Vui lòng chọn ảnh đại diện.")
               .Must(file => file.Length > 0)
               .WithMessage("Tệp ảnh không được để trống.")
               .Must(file => file.Length <= 5 * 1024 * 1024)
               .WithMessage("Ảnh đại diện không được vượt quá 5 MB.")
               .Must(file => AllowedExtensions.Contains(
                   Path.GetExtension(file.FileName),
                   StringComparer.OrdinalIgnoreCase))
               .WithMessage("Chỉ hỗ trợ định dạng JPG, JPEG, PNG hoặc WEBP.")
               .Must(file => AllowedContentTypes.Contains(
                   file.ContentType,
                   StringComparer.OrdinalIgnoreCase))
               .WithMessage("Content-Type của tệp ảnh không hợp lệ.");

            //RuleFor(x => x.AvatarUrl)
            //    .Cascade(CascadeMode.Stop)
            //    .NotNull()
            //    .WithMessage("Vui lòng chọn ảnh đại diện.")
            //    .Must(file => file.Length > 0)
            //    .WithMessage("Tệp ảnh không được để trống.");

        }

        private bool BeAValidUrl(IFormFile file)
        {
            return Uri.TryCreate(file.FileName, UriKind.Absolute, out _);
        }
    }
}
