using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Negotiates
{
    public sealed class SendMessageRequestValidator
    : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.MessageContent)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Nội dung tin nhắn không được để trống.")
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Nội dung tin nhắn không được chỉ chứa khoảng trắng.")
                .MaximumLength(2000)
                .WithMessage("Nội dung tin nhắn không được vượt quá 2000 ký tự.");

            RuleFor(x => x.ClientMessageId)
                .NotEmpty()
                .WithMessage("ClientMessageId không được để trống.");
        }
    }
}
