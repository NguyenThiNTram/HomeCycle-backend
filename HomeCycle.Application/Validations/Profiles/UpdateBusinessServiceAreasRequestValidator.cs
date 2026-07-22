using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateBusinessServiceAreasRequestValidator : AbstractValidator<UpdateBusinessServiceAreasRequest>
    {
        public UpdateBusinessServiceAreasRequestValidator()
        {
            RuleFor(x => x.ServiceAreas)
                .NotNull().WithMessage("Danh sách khu vực hoạt động không được null.");

            RuleForEach(x => x.ServiceAreas).ChildRules(area =>
            {
                area.RuleFor(a => a.City)
                    .NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.")
                    .MaximumLength(100).WithMessage("Tên Tỉnh/Thành phố quá dài.");

                area.RuleFor(a => a.District)
                    .NotEmpty().WithMessage("Quận/Huyện không được để trống.")
                    .MaximumLength(100).WithMessage("Tên Quận/Huyện quá dài.");

                area.RuleFor(a => a.Ward)
                    .NotEmpty().WithMessage("Phường/Xã không được để trống.")
                    .MaximumLength(100).WithMessage("Tên Phường/Xã quá dài.");
            });
        }
    }
}
