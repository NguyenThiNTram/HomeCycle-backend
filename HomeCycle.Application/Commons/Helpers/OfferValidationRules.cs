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
                .WithMessage("Giá đề nghị phải lớn hơn 0.")
                .PrecisionScale(
                    precision: 18,
                    scale: 2,
                    ignoreTrailingZeros: true)
                .WithMessage("Giá đề nghị được có tối đa 18 chữ số và 2 chữ số thập phân.");

            validator.RuleFor(quantityExpression)
                .GreaterThan(0)
                .WithMessage("Số lượng đề nghị phải lớn hơn 0.");
        }
    }
}
