using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Reviews
{
    public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
    {
        public const int MaxImages = 3;

        public CreateReviewRequestValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating phải nằm trong khoảng từ 1 đến 5 sao.");

            RuleFor(x => x.Comment)
                .MaximumLength(2000).WithMessage("Comment không được vượt quá 2000 ký tự.");

            RuleFor(x => x.Images)
                .Must(images => images == null || images.Count(f => f != null && f.Length > 0) <= MaxImages)
                .WithMessage($"Tối đa {MaxImages} ảnh cho một đánh giá.");
        }
    }
}
