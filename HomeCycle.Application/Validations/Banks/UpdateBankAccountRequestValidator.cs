using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Banks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Banks
{
    public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
    {
        public UpdateBankAccountRequestValidator()
        {
            RuleFor(x => x.BankCode)
                .NotEmpty().WithMessage("Mã ngân hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã ngân hàng không vượt quá 20 ký tự.");

            RuleFor(x => x.BankName)
                .NotEmpty().WithMessage("Tên ngân hàng không được để trống.")
                .MaximumLength(255).WithMessage("Tên ngân hàng không vượt quá 255 ký tự.");

            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Số tài khoản không được để trống.")
                .Matches(@"^[0-9A-Za-z]{6,30}$").WithMessage("Số tài khoản hợp lệ từ 6-30 ký tự số/chữ.");

            RuleFor(x => x.AccountName)
                .NotEmpty().WithMessage("Tên chủ tài khoản không được để trống.")
                .MaximumLength(255).WithMessage("Tên chủ tài khoản không vượt quá 255 ký tự.");
        }
    }
}
