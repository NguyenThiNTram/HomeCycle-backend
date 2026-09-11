using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IReadOnlyList<wallet_transaction>> GetByReferenceAsync(
            ReferenceType referenceType,
            Guid referenceId,
            CancellationToken ct = default)
        {
            var entities = await _db.Wallet_Transactions
                .AsNoTracking()
                .Where(x => x.ReferenceType == (int)referenceType && x.ReferenceId == referenceId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.WalletTransactionId)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }
    }
}
