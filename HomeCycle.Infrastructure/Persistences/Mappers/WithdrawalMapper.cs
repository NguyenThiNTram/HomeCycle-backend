using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class WithdrawalMapper
    {
        public static withdrawal ToDomain(this Withdrawal entity)
        {
            if (entity == null) return null;
            return new withdrawal
            {
                WithdrawalId = entity.WithdrawalId,
                WalletId = entity.WalletId,
                UserBankId = entity.UserBankId,
                Amount = entity.Amount,
                WithdrawalStatus = entity.WithdrawalStatus,
                RequestedAt = entity.RequestedAt
            };
        }
        public static Withdrawal ToInfrastructure(this withdrawal entity)
        {
            if (entity == null) return null;
            return new Withdrawal
            {
                WithdrawalId = entity.WithdrawalId,
                WalletId = entity.WalletId,
                UserBankId = entity.UserBankId,
                Amount = entity.Amount,
                WithdrawalStatus = entity.WithdrawalStatus,
                RequestedAt = entity.RequestedAt
            };
        }
    }
}
