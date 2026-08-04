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
    public class WalletRepository : IWalletRepository
    {
        private readonly HomeCycleDbContext _db;
        public WalletRepository(HomeCycleDbContext db) => _db = db;

        public async Task<wallet?> GetByUserIdAndTypeAsync(Guid userId, WalletTypeEnum walletType, CancellationToken ct = default)
        {
            var entity = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.WalletType == (int)walletType, ct);
            return entity?.ToDomain();
        }

        public Task UpdateAsync(wallet wallet, CancellationToken ct = default)
        {
            _db.Wallets.Update(wallet.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
