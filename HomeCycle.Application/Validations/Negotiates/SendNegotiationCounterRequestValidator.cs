using FluentValidation;
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
            RuleFor(x => x.OfferPrice)
                .GreaterThan(0)
                .WithMessage("Giá đề nghị phải lớn hơn 0.")
                .PrecisionScale(18, 2, false)
                .WithMessage("Giá đề nghị không được vượt quá 18 chữ số và 2 chữ số thập phân.");

            RuleFor(x => x.OfferQuantity)
                .GreaterThan(0)
                .WithMessage("Số lượng đề nghị phải lớn hơn 0.");

            //RuleFor(x => x.MessageContent)
            //    .MaximumLength(1000)
            //    .WithMessage("Nội dung tin nhắn không được vượt quá 1000 ký tự.")
            //    .When(x => !string.IsNullOrWhiteSpace(x.MessageContent));
        }
    }
}
