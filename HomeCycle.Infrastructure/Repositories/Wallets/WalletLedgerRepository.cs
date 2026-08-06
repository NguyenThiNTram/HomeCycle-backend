using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Wallets
{
    public class WalletLedgerRepository : IWalletLedgerRepository
    {
        private readonly HomeCycleDbContext _db;
        public WalletLedgerRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(wallet_ledger ledger, CancellationToken ct = default)
        {

            await _db.Wallet_Ledgers.AddAsync(ledger.ToInfrastructure(), ct);
        }
    }
}
