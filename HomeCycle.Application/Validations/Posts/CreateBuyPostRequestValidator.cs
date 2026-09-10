using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.Validations.Products;

namespace HomeCycle.Application.Validations.Posts;

public sealed class CreateBuyPostRequestValidator : AbstractValidator<CreateBuyPostRequest>
{
    public CreateBuyPostRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.StreetAddress).MaximumLength(500);
        RuleFor(x => x.Ward).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.PriorityLevel).IsInEnum().When(x => x.PriorityLevel.HasValue);
        RuleFor(x => x.PriceFrom).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).When(x => x.PriceFrom.HasValue);
        RuleFor(x => x.PriceTo).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).When(x => x.PriceTo.HasValue);
        RuleFor(x => x).Must(x => !x.PriceFrom.HasValue || !x.PriceTo.HasValue || x.PriceFrom <= x.PriceTo)
            .WithMessage("Giá tối thiểu không được lớn hơn giá tối đa.");
        RuleFor(x => x.ExpiryDate).Must(x => !x.HasValue || (x.Value.ToUniversalTime() > DateTime.UtcNow && x.Value.ToUniversalTime() <= DateTime.UtcNow.AddMonths(6)))
            .WithMessage("Thời hạn tin phải ở tương lai và không quá 6 tháng.");
        RuleFor(x => x).Custom((x, context) => {
            var result = new ProductRequirementRequestValidator().Validate(new ProductRequirementRequest {
                ProductName = x.Title, CategoryId = x.CategoryId, ProductTypeId = x.ProductTypeId, BrandId = x.BrandId,
                FunctionalityStatus = x.FunctionalityStatus, UsageDuration = x.UsageDuration, DamageLevel = x.DamageLevel,
                AttributeValues = x.AttributeValues ?? []
            });
            foreach (var error in result.Errors) context.AddFailure(error);
        });
    }
}
public sealed class UpdateBuyPostRequestValidator : AbstractValidator<UpdateBuyPostRequest>
{
    public UpdateBuyPostRequestValidator()
    {
        RuleFor(x => x.ChangedProperties).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255).When(x => x.ChangedProperties.Contains(nameof(x.Title)));
        RuleFor(x => x.Description).NotEmpty().When(x => x.ChangedProperties.Contains(nameof(x.Description)));
        RuleFor(x => x.Quantity).NotNull().GreaterThan(0).When(x => x.ChangedProperties.Contains(nameof(x.Quantity)));
        RuleFor(x => x.ExpiryDate).NotNull().When(x => x.ChangedProperties.Contains(nameof(x.ExpiryDate)));
        // Validate the merged document with CreateBuyPostRequestValidator in PostService.
    }
}
