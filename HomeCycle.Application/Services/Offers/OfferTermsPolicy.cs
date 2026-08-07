using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Services.Offers;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Offers
{
    public sealed class OfferTermsPolicy : IOfferTermsPolicy
    {
        private const decimal MinPriceFactor = 0.2m;
        private const decimal MaxPriceFactor = 3m;

        public Error? Validate(post post, decimal offerPrice, int offerQuantity)
        {
            if (offerQuantity <= 0)
                return OfferErrors.InvalidQuantity;

            if (offerQuantity > post.RemainingQuantity)
            {
                return OfferErrors.QuantityExceedsRemaining(
                    offerQuantity,
                    post.RemainingQuantity);
            }

            if (!post.BasePrice.HasValue)
                return OfferErrors.PriceOutOfRange(0, 0);

            var minPrice = post.BasePrice.Value * MinPriceFactor;
            var maxPrice = post.BasePrice.Value * MaxPriceFactor;

            return offerPrice < minPrice || offerPrice > maxPrice
                ? OfferErrors.PriceOutOfRange(minPrice, maxPrice)
                : null;
        }
    }
}
