using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Offers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Offers
{
    public sealed class CounterInitialOfferRequestValidator
        : AbstractValidator<CounterInitialOfferRequest>
    {
        public CounterInitialOfferRequestValidator()
        {
            RuleFor(x => x.OfferPrice)
                .GreaterThan(0)
                .WithMessage("Giá đề nghị phải lớn hơn 0.");

            RuleFor(x => x.OfferQuantity)
                .GreaterThan(0)
                .WithMessage("Số lượng đề nghị phải lớn hơn 0.");
        }
    }
}
