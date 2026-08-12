using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWalletRepository
    {
        Task AddAsync(wallet wallet, CancellationToken ct = default);
        Task<wallet?> GetByUserIdAndTypeAsync(Guid userId, WalletTypeEnum walletType, CancellationToken ct = default);
        Task UpdateAsync(wallet wallet, CancellationToken ct = default);
    }
}
