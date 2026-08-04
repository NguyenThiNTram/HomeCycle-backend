using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.Validations.Files;
using HomeCycle.Application.Validations.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Posts
{
    public class CreateBuyPostRequestValidator : AbstractValidator<CreateBuyPostRequest>
    {
        public CreateBuyPostRequestValidator()
        {
            // Kế thừa các rule chung từ CreatePostRequestValidator
            Include(new CreatePostRequestValidator());

            RuleFor(x => x.ExpectedPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá mong muốn phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.Requirement)
                .NotNull().WithMessage("Yêu cầu sản phẩm thu mua không được để trống.")
                .SetValidator(new ProductRequirementRequestValidator());

            // Ảnh là không bắt buộc đối với tin thu mua
            RuleForEach(x => x.Medias)
                .SetValidator(new FormFileValidator());
        }
    }

    public class UpdateBuyPostRequestValidator : AbstractValidator<UpdateBuyPostRequest>
    {
        public UpdateBuyPostRequestValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).When(x => x.Quantity.HasValue)
                .WithMessage("Số lượng phải lớn hơn 0.");

            RuleFor(x => x.StreetAddress)
                .MaximumLength(500).WithMessage("Địa chỉ không được vượt quá 500 ký tự.");

            RuleFor(x => x.Ward)
                .MaximumLength(255).WithMessage("Phường/Xã không được vượt quá 255 ký tự.");

            RuleFor(x => x.City)
                .MaximumLength(255).WithMessage("Thành phố/Tỉnh không được vượt quá 255 ký tự.");

            RuleFor(x => x.ExpectedPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá mong muốn phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.Requirement)
                .NotNull().WithMessage("Yêu cầu sản phẩm thu mua không được để trống.");

            RuleForEach(x => x.Medias)
                .SetValidator(new FormFileValidator());
        }
    }
}
