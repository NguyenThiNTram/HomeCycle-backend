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

            RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(255);
            RuleFor(x => x.TaxCode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.BusinessAddress).NotEmpty();
            RuleFor(x => x.Ward).NotEmpty();
            RuleFor(x => x.City).NotEmpty();


            RuleFor(x => x.BusinessRegistrationCertificate)
                .NotNull().WithMessage("Giấy đăng ký kinh doanh là bắt buộc mỗi lần cập nhật.")
                .SetValidator(new FormFileValidator());
        }
    }
}
