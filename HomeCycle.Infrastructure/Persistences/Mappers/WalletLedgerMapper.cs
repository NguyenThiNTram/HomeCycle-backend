using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class WalletLedgerMapper
    {
        public static wallet_ledger ToDomain(this Wallet_Ledger entity)
        {
            if (entity == null) return null;
            return new wallet_ledger
            {
                LedgerId = entity.LedgerId,
                WalletTransactionId = entity.WalletTransactionId,
                WalletId = entity.WalletId,
                Direction = entity.Direction,
                Amount = entity.Amount,
                BalanceAfter = entity.BalanceAfter,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Wallet_Ledger ToInfrastructure(this wallet_ledger entity)
        {
            if (entity == null) return null;
            return new Wallet_Ledger
            {
                LedgerId = entity.LedgerId,
                WalletTransactionId = entity.WalletTransactionId,
                WalletId = entity.WalletId,
                Direction = entity.Direction,
                Amount = entity.Amount,
                BalanceAfter = entity.BalanceAfter,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
