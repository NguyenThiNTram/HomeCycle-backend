using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWalletTransactionRepository
    {
        Task AddAsync(wallet_transaction transaction, CancellationToken ct = default);
    }
}
