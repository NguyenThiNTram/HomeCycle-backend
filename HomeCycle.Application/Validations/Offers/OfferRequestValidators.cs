using FluentValidation;
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
                .WithMessage("PostId is required.");

            RuleFor(x => x.OfferPrice)
                .GreaterThan(0)
                .WithMessage("Offer price must be greater than 0.");

            RuleFor(x => x.OfferQuantity)
                .GreaterThan(0)
                .WithMessage("Offer quantity must be greater than 0.");
        }
    }

    public class UpdateOfferRequestValidator : AbstractValidator<UpdateOfferRequest>
    {
        public UpdateOfferRequestValidator()
        {
            RuleFor(x => x.OfferPrice)
                .GreaterThan(0)
                .WithMessage("Offer price must be greater than 0.");

            RuleFor(x => x.OfferQuantity)
                .GreaterThan(0)
                .WithMessage("Offer quantity must be greater than 0.");
        }
    }
}
