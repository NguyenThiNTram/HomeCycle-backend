using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Banks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Users
{
    public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
    {
        public UpdateBankAccountRequestValidator()
        {
            RuleFor(x => x.BankCode)
                .NotEmpty().WithMessage("Mã ngân hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã ngân hàng không hợp lệ.");

            RuleFor(x => x.BankName)
                .NotEmpty().WithMessage("Tên ngân hàng không được để trống.")
                .MaximumLength(255).WithMessage("Tên ngân hàng quá dài.");

            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Số tài khoản không được để trống.")
                .Matches(@"^[0-9A-Za-z]+$").WithMessage("Số tài khoản chỉ chứa chữ và số.")
                .MaximumLength(50).WithMessage("Số tài khoản quá dài.");

            RuleFor(x => x.AccountName)
                .NotEmpty().WithMessage("Tên chủ tài khoản không được để trống.")
                .MaximumLength(255).WithMessage("Tên chủ tài khoản quá dài.");
        }
    }
}
