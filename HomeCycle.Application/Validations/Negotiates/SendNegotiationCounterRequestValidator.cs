using FluentValidation;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Negotiates
{
    public sealed class SendNegotiationCounterRequestValidator
        : AbstractValidator<SendNegotiationCounterRequest>
    {
        public SendNegotiationCounterRequestValidator()
        {
            this.AddOfferTermsRules(
               x => x.OfferPrice,
               x => x.OfferQuantity);
        }
    }
}
