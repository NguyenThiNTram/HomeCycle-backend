using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class ReviewPersonalIdentityRequestValidator
    : AbstractValidator<ReviewPersonalIdentityRequest>
    {
        public ReviewPersonalIdentityRequestValidator()
        {
            RuleFor(x => x.Decision)
                .Must(x => x is VerifyStatus.Verified or VerifyStatus.Rejected)
                .WithMessage("Quyết định chỉ được là Verified hoặc Rejected.");

            When(x => x.Decision == VerifyStatus.Rejected, () =>
            {
                RuleFor(x => x.RejectReason)
                    .NotEmpty()
                    .WithMessage("Vui lòng nhập lý do từ chối.")
                    .MaximumLength(1000)
                    .WithMessage("Lý do từ chối không được vượt quá 1000 ký tự.");
            });
        }
    }
}
