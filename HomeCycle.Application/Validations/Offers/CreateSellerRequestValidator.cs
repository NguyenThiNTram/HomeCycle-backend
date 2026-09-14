using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.Commons.Helpers;
namespace HomeCycle.Application.Validations.Offers;
public sealed class CreateSellerRequestValidator : AbstractValidator<CreateSellerRequest>
{
    public CreateSellerRequestValidator() { RuleFor(x => x.SellPostId).NotEmpty(); this.AddOfferTermsRules(x => x.OfferPrice, x => x.OfferQuantity); }
}
public sealed class OfferSearchRequestValidator : AbstractValidator<OfferSearchRequest>
{
    public OfferSearchRequestValidator() {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.PostId).NotEmpty().When(x => x.PostId.HasValue);
        RuleFor(x => x.BuyPostId).NotEmpty().When(x => x.BuyPostId.HasValue);
    }
}
