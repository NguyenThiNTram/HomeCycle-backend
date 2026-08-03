using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateApprovedBusinessProfileRequestValidator : AbstractValidator<UpdateApprovedBusinessProfileRequest>
    {
        public UpdateApprovedBusinessProfileRequestValidator()
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty().WithMessage("Tên doanh nghiệp không được để trống.")
                .MaximumLength(255).WithMessage("Tên doanh nghiệp không được vượt quá 255 ký tự.");

            RuleFor(x => x.TaxCode)
                .NotEmpty().WithMessage("Mã số thuế không được để trống.")
                .Matches(@"^[0-9]{10}(-[0-9]{3})?$").WithMessage("Mã số thuế không đúng định dạng (10 hoặc 13 số).");

            RuleFor(x => x.BusinessAddress)
                .NotEmpty().WithMessage("Địa chỉ kinh doanh không được để trống.");

            RuleFor(x => x.Ward)
                .NotEmpty().WithMessage("Phường/Xã không được để trống.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.");

            RuleFor(x => x.IdentityNumber)
                .NotEmpty().WithMessage("Số CCCD/CMND không được để trống.")
                .Matches(@"^[0-9]{9,12}$").WithMessage("Số CCCD/CMND phải từ 9 đến 12 chữ số.");
        }
    }
}
