using FluentValidation;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.DTOs.Requests.Offers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Offers
{
    public class CreateOfferRequestValidator : AbstractValidator<CreateOfferRequest>
    {
        public CreateOfferRequestValidator()
        {
            RuleFor(x => x.PostId)
                .NotEmpty()
                .WithMessage("PostId is not empty");

            this.AddOfferTermsRules(
                x => x.OfferPrice,
                x => x.OfferQuantity);
        }
    }

    public class UpdateOfferRequestValidator : AbstractValidator<UpdateOfferRequest>
    {
        public UpdateOfferRequestValidator()
        {
            RuleFor(x => x)
             .Must(x =>
                 x.OfferPrice.HasValue ||
                 x.OfferQuantity.HasValue)
             .WithMessage(
                 "Must provide at least a price or quantity to update");

            RuleFor(x => x.OfferPrice)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0m)
                .WithMessage("Offer price must be greater than 0")
                .PrecisionScale(
                    precision: 18,
                    scale: 2,
                    ignoreTrailingZeros: true)
                .WithMessage(
                    "Offer price can have a maximum of 18 digits and 2 decimal places")
                .When(x => x.OfferPrice.HasValue);

            RuleFor(x => x.OfferQuantity)
                .GreaterThan(0)
                .WithMessage("Offer quantity must be greater than 0")
                .When(x => x.OfferQuantity.HasValue);
        }
    }

    public class AcceptOfferRequestValidator : AbstractValidator<AcceptOfferRequest>
    {
        public AcceptOfferRequestValidator()
        {
            RuleFor(x => x.Version)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Version is required or must be greater than 0");
        }
    }
}
