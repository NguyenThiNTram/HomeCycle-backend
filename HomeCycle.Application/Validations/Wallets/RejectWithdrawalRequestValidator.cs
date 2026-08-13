using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Wallets
{
    public sealed class RejectWithdrawalRequestValidator : AbstractValidator<RejectWithdrawalRequest>
    {
        public RejectWithdrawalRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Lý do từ chối không được để trống.")
                .MaximumLength(500).WithMessage("Lý do từ chối tối đa 500 ký tự.");
        }
    }
}
