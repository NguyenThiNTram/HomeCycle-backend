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
            Include(new CreatePostRequestValidator());

            RuleFor(x => x.ExpectedPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá mong muốn phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.Requirement)
                .NotNull().WithMessage("Yêu cầu sản phẩm thu mua không được để trống.");

            RuleForEach(x => x.Medias)
                .SetValidator(new FormFileValidator());
        }
    }
}
