using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Validations.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Posts
{
    public static class PostValidationLimits
    {
        public const decimal MinPrice = 10_000m;
        public const decimal MaxPrice = 500_000_000m;
        public const int MaxDescriptionLength = 5_000;
    }

    public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostRequestValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

            RuleFor(x => x.StreetAddress)
                .MaximumLength(500).WithMessage("Địa chỉ không được vượt quá 500 ký tự.");

            RuleFor(x => x.Ward)
                .MaximumLength(100).WithMessage("Phường/Xã không được vượt quá 100 ký tự.");

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("Thành phố/Tỉnh không được vượt quá 100 ký tự.");

            RuleFor(x => x.PriorityLevel);
        }

    }

}
