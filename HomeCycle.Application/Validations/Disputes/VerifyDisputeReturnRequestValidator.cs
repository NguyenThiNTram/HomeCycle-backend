using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class VerifyDisputeReturnRequestValidator : AbstractValidator<VerifyDisputeReturnRequest>
    {
        public VerifyDisputeReturnRequestValidator()
        {
            RuleFor(x => x.ModeratorNote)
                .NotEmpty()
                .WithMessage("Moderator phải ghi rõ kết quả xác minh hoàn trả.")
                .MinimumLength(10)
                .WithMessage("Kết quả xác minh hoàn trả phải có ít nhất 10 ký tự.")
                .MaximumLength(2000)
                .WithMessage("Kết quả xác minh hoàn trả không được vượt quá 2000 ký tự.");
        }
    }
}
