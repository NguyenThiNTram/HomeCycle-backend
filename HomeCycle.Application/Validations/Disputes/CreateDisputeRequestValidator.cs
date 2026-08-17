using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.Validations.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class CreateDisputeRequestValidator
         : AbstractValidator<CreateDisputeRequest>
    {
        public CreateDisputeRequestValidator()
        {
            RuleFor(x => x.TargetType)
                .IsInEnum()
                .WithMessage("Loại đối tượng tranh chấp không hợp lệ.");

            RuleFor(x => x.TargetId)
                .NotEmpty()
                .WithMessage("TargetId không được để trống.");

            RuleFor(x => x.Category)
                .IsInEnum()
                .WithMessage("Loại tranh chấp không hợp lệ.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Mô tả tranh chấp không được để trống.")
                .MinimumLength(10)
                .WithMessage("Mô tả tranh chấp phải có ít nhất 10 ký tự.")
                .MaximumLength(2000)
                .WithMessage("Mô tả tranh chấp không được vượt quá 2000 ký tự.");

            RuleFor(x => x.EvidenceImages)
                .NotNull()
                .Must(files =>
                    files != null &&
                    files.Count >= 3 &&
                    files.Count <= 5)
                .WithMessage("Phải cung cấp từ 3 đến 5 ảnh bằng chứng.");

            RuleForEach(x => x.EvidenceImages)
                .SetValidator(new FormFileValidator());
        }
    }
}
