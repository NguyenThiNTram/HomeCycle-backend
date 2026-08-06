using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWalletLedgerRepository
    {
        Task AddAsync(wallet_ledger ledger, CancellationToken ct = default);
    }
}
