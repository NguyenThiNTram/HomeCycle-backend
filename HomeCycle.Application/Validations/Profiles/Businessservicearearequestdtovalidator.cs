using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class BusinessServiceAreaRequestDtoValidator : AbstractValidator<BusinessServiceAreaRequestDto>
    {
        public BusinessServiceAreaRequestDtoValidator()
        {
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.")
                .MaximumLength(100).WithMessage("Tên Tỉnh/Thành phố quá dài.");

            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Địa chỉ/Đường không được để trống.")
                .MaximumLength(255).WithMessage("Địa chỉ/Đường quá dài.");

            RuleFor(x => x.Ward)
                .NotEmpty().WithMessage("Phường/Xã không được để trống.")
                .MaximumLength(100).WithMessage("Tên Phường/Xã quá dài.");
        }
    }
}
