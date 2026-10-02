using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class RespondDisputeRequestValidator : AbstractValidator<RespondDisputeRequest>
    {
        public RespondDisputeRequestValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ResponseType)
                .IsInEnum()
                .WithMessage("Loại phản hồi không hợp lệ.");

            When(
                x => x.ResponseType is DisputeResponseType.Rebut or DisputeResponseType.Statement,
                () =>
                {
                    RuleFor(x => x.Content)
                        .NotEmpty()
                        .WithMessage("Nội dung phản hồi là bắt buộc.")
                        .MinimumLength(10)
                        .WithMessage("Nội dung phản hồi phải có ít nhất 10 ký tự.")
                        .MaximumLength(2000)
                        .WithMessage("Nội dung phản hồi không được vượt quá 2000 ký tự.");
                });

            When(
                x => x.ResponseType == DisputeResponseType.Accept,
                () =>
                {
                    RuleFor(x => x.Content)
                        .MaximumLength(2000)
                        .WithMessage("Nội dung phản hồi không được vượt quá 2000 ký tự.");

                    RuleFor(x => x.EvidenceImages)
                        .Must(files => files.Count == 0)
                        .WithMessage("Phản hồi chấp nhận không cần evidence.");
                });

            RuleFor(x => x.EvidenceImages)
                .NotNull()
                .WithMessage("Danh sách evidence không được null.")
                .Must(files => files != null && files.Count <= 5)
                .WithMessage("Tối đa 5 ảnh evidence cho phản hồi.");
        }
    }
}
