using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class WalletMapper
    {
        public static wallet ToDomain(this Wallet entity)
        {
            if (entity == null) return null;
            return new wallet
            {
                WalletId = entity.WalletId,
                UserId = entity.UserId,
                WalletType = entity.WalletType,
                AvailableBalance = entity.AvailableBalance,
                HoldBalance = entity.HoldBalance,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Wallet ToInfrastructure(this wallet entity)
        {
            if (entity == null) return null;
            return new Wallet
            {
                WalletId = entity.WalletId,
                UserId = entity.UserId,
                WalletType = entity.WalletType,
                AvailableBalance = entity.AvailableBalance,
                HoldBalance = entity.HoldBalance,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
