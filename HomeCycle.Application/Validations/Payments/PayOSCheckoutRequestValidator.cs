using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Payments
{
    public class PayOSCheckoutRequestValidator : AbstractValidator<PayOSCheckoutRequest>
    {
        public PayOSCheckoutRequestValidator()
        {

            RuleFor(x => x.ReturnUrl)
                .NotEmpty().WithMessage("returnUrl là bắt buộc.")
                .MaximumLength(2048).WithMessage("returnUrl vượt quá độ dài cho phép.");

            RuleFor(x => x.CancelUrl)
                .NotEmpty().WithMessage("cancelUrl là bắt buộc.")
                .MaximumLength(2048).WithMessage("cancelUrl vượt quá độ dài cho phép.");
        }
    }
}
