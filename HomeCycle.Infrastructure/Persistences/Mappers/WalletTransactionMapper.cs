using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class WalletTransactionMapper
    {
        public static wallet_transaction ToDomain(this Wallet_Transaction entity)
        {
            if (entity == null) return null;
            return new wallet_transaction
            {
                WalletTransactionId = entity.WalletTransactionId,
                FromWalletId = entity.FromWalletId,
                ToWalletId = entity.ToWalletId,
                PaymentId = entity.PaymentId,
                ReferenceId = entity.ReferenceId,
                ReferenceType = entity.ReferenceType,
                TransactionType = entity.TransactionType,
                Amount = entity.Amount,
                WalletTransactionStatus = entity.WalletTransactionStatus,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Wallet_Transaction ToInfrastructure(this wallet_transaction entity)
        {
            if (entity == null) return null;
            return new Wallet_Transaction
            {
                WalletTransactionId = entity.WalletTransactionId,
                FromWalletId = entity.FromWalletId,
                ToWalletId = entity.ToWalletId,
                PaymentId = entity.PaymentId,
                ReferenceId = entity.ReferenceId,
                ReferenceType = entity.ReferenceType,
                TransactionType = entity.TransactionType,
                Amount = entity.Amount,
                WalletTransactionStatus = entity.WalletTransactionStatus,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
