using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Disputes
{
    public class DisputeModeratorDecisionRequestValidator : AbstractValidator<DisputeModeratorDecisionRequest>
    {
        public DisputeModeratorDecisionRequestValidator()
        {
            RuleFor(x => x.ModeratorNote)
                .NotEmpty()
                .WithMessage("Moderator phải ghi rõ kết luận xử lý tranh chấp.")
                .MinimumLength(10)
                .WithMessage("Kết luận xử lý tranh chấp phải có ít nhất 10 ký tự.")
                .MaximumLength(2000)
                .WithMessage("Kết luận xử lý tranh chấp không được vượt quá 2000 ký tự.");
        }
    }

}
