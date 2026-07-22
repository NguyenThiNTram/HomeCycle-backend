using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateBusinessProfileRequestValidator : AbstractValidator<UpdateBusinessProfileRequest>
    {
        public UpdateBusinessProfileRequestValidator()
        {
            RuleFor(x => x.IdentityNumber)
                .NotEmpty().WithMessage("Số CCCD/CMND không được để trống.")
                .Matches(@"^(\d{9}|\d{12})$").WithMessage("Số CCCD/CMND phải đúng 9 hoặc 12 chữ số.");

            When(x => !string.IsNullOrEmpty(x.TaxCode), () => {
                RuleFor(x => x.TaxCode)
                    .Matches(@"^\d{10}(\d{3})?$").WithMessage("Mã số thuế không hợp lệ (10 hoặc 13 chữ số).");
            });

            When(x => !string.IsNullOrEmpty(x.BusinessName), () => {
                RuleFor(x => x.BusinessName).MaximumLength(255).WithMessage("Tên doanh nghiệp không vượt quá 255 ký tự.");
            });

            When(x => !string.IsNullOrEmpty(x.FullName), () => {
                RuleFor(x => x.FullName).MaximumLength(255).WithMessage("Tên người đại diện không vượt quá 255 ký tự.");
            });
        }
    }
}
