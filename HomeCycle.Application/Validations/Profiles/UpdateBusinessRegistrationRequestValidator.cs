using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.Validations.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateBusinessRegistrationRequestValidator : AbstractValidator<UpdateBusinessRegistrationRequest>
    {
        public UpdateBusinessRegistrationRequestValidator()
        {

            //RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(255);
            //RuleFor(x => x.TaxCode).NotEmpty().MaximumLength(50);
            //RuleFor(x => x.BusinessAddress).NotEmpty();
            //RuleFor(x => x.Ward).NotEmpty();
            //RuleFor(x => x.City).NotEmpty();

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Tên doanh nghiệp không được để trống.")
                .MaximumLength(255)
                .WithMessage(
                    "Tên doanh nghiệp không được vượt quá 255 ký tự.");

            RuleFor(x => x.TaxCode)
                .NotEmpty()
                .WithMessage("Mã số thuế không được để trống.")
                .MaximumLength(50)
                .WithMessage(
                    "Mã số thuế không được vượt quá 50 ký tự.");

            RuleFor(x => x.BusinessAddress)
                .NotEmpty()
                .WithMessage(
                    "Địa chỉ doanh nghiệp không được để trống.");

            RuleFor(x => x.Ward)
                .NotEmpty()
                .WithMessage("Phường/xã không được để trống.");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("Tỉnh/thành phố không được để trống.");

            RuleFor(x => x.BusinessRegistrationCertificate)
                .NotNull()
                .WithMessage("Giấy đăng ký kinh doanh là bắt buộc mỗi lần cập nhật.");


            //RuleFor(x => x.BusinessRegistrationCertificate)
            //    .NotNull().WithMessage("Giấy đăng ký kinh doanh là bắt buộc mỗi lần cập nhật.")
            //    .SetValidator(new FormFileValidator());
        }
    }
}
