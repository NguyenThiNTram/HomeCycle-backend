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
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly HomeCycleDbContext _db;
        public WalletTransactionRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(wallet_transaction transaction, CancellationToken ct = default)
        {
            await _db.Wallet_Transactions.AddAsync(transaction.ToInfrastructure(), ct);
        }
    }
}
