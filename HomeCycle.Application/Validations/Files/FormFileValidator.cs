using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Files
{
    public class FormFileValidator : AbstractValidator<IFormFile>
    {
        // Các đuôi file ảnh được phép upload
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Giới hạn dung lượng tối đa 1 file (ví dụ: 5MB)
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;

        public FormFileValidator()
        {
            RuleFor(x => x)
                .NotNull().WithMessage("Tệp tin không được để trống.");

            RuleFor(x => x.Length)
                .GreaterThan(0).WithMessage("Tệp tin không được có dung lượng bằng 0.")
                .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("Dung lượng mỗi ảnh không được vượt quá 5MB.");

            RuleFor(x => x.FileName)
                .Must(fileName =>
                {
                    if (string.IsNullOrEmpty(fileName)) return false;
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    return AllowedExtensions.Contains(extension);
                })
                .WithMessage("Định dạng file không được hỗ trợ (chỉ chấp nhận .jpg, .jpeg, .png, .webp).");
        }
    }
}
