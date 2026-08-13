using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Wallets
{
    public sealed class CreateWithdrawalRequestValidator : AbstractValidator<CreateWithdrawalRequest>
    {
        public CreateWithdrawalRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Số tiền rút phải lớn hơn 0.")
                // Chặn số lẻ dưới đơn vị VNĐ (không có xu ở VN) — tránh giá trị kiểu 100000.5
                .Must(x => x == Math.Floor(x)).WithMessage("Số tiền rút phải là số nguyên (đơn vị VNĐ).");
        }
    }
}
