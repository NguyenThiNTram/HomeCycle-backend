using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Banks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Banks
{
    public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
    {
        public UpdateBankAccountRequestValidator()
        {
            When(x =>
               !string.IsNullOrWhiteSpace(x.BankCode) ||
               !string.IsNullOrWhiteSpace(x.BankName) ||
               !string.IsNullOrWhiteSpace(x.AccountNumber) ||
               !string.IsNullOrWhiteSpace(x.AccountName),
               () =>
               {
                   RuleFor(x => x.BankCode).NotEmpty().MaximumLength(20);
                   RuleFor(x => x.BankName).NotEmpty().MaximumLength(255);

                   RuleFor(x => x.AccountNumber)
                       .NotEmpty().WithMessage("Số tài khoản không được để trống.")
                       .Matches(@"^[0-9A-Za-z]+$").WithMessage("Số tài khoản chỉ chứa chữ và số.")
                       .MaximumLength(50);

                   // RULE MỚI: Chỉ yêu cầu NotEmpty và MaximumLength. Không còn so sánh chéo.
                   RuleFor(x => x.AccountName)
                        .NotEmpty().WithMessage("Tên chủ tài khoản không được để trống.")
                        .MaximumLength(255).WithMessage("Tên chủ tài khoản quá dài.");
               }
            );
        }
    }
}
