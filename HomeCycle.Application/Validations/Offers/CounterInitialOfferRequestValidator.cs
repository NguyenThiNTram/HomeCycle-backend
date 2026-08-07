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
    public sealed class CounterInitialOfferRequestValidator
        : AbstractValidator<CounterInitialOfferRequest>
    {
        private const int MaxMessageLength = 1000;

        public CounterInitialOfferRequestValidator()
        {
            this.AddOfferTermsRules(
                x => x.OfferPrice,
                x => x.OfferQuantity);
        }
    }
}
