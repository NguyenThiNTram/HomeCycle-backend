using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public static class OfferValidationRules
    {
        public static void AddOfferTermsRules<T>(
            this AbstractValidator<T> validator,
            Expression<Func<T, decimal>> priceExpression,
            Expression<Func<T, int>> quantityExpression)
        {
            validator.RuleFor(priceExpression)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0m)
                .WithMessage("Price must be greater than 0")
                .PrecisionScale(
                    precision: 18,
                    scale: 2,
                    ignoreTrailingZeros: true)
                .WithMessage(
                    "Offer price must not exceed 18 digits, " +
                    "with a maximum of 2 decimal places");

            validator.RuleFor(quantityExpression)
                .GreaterThan(0)
                .WithMessage("Offer quantity must be greater than 0");
        }
    }
}
