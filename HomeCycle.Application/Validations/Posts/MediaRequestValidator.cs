using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Posts
{
    public class MediaRequestValidator : AbstractValidator<MediaRequest>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Dung lượng file tối đa cho phép (10MB)
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public MediaRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("Tệp đính kèm không được để trống.")
                .Must(file => file != null && file.Length > 0)
                .WithMessage("Tệp tải lên không được rỗng.")
                .Must(file => file != null && file.Length <= MaxFileSizeBytes)
                .WithMessage("Dung lượng hình ảnh không được vượt quá 10MB.")
                .Must(file => file != null && AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                .WithMessage("Chỉ hỗ trợ các định dạng hình ảnh: .jpg, .jpeg, .png, .webp");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Thứ tự hiển thị (DisplayOrder) phải lớn hơn hoặc bằng 0.");

        }
    }
}
