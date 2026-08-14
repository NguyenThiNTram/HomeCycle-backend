using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
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

        public async Task AddAsync(wallet wallet, CancellationToken ct = default)
        {
            await _db.Wallets.AddAsync(wallet.ToInfrastructure(), ct);
        }

        public async Task<wallet?> GetUserWalletForUpdateAsync(Guid userId, CancellationToken ct = default)
        {
            var entity = await _db.Wallets
                .FromSqlInterpolated($"SELECT * FROM public.\"Wallet\" WHERE \"UserId\" = {userId} FOR UPDATE")
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<wallet?> GetSystemWalletForUpdateAsync(SystemWalletPurpose purpose, CancellationToken ct = default)
        {
            int systemWalletType = (int)WalletTypeEnum.System;
            int purposeValue = (int)purpose;

            var entity = await _db.Wallets
                .FromSqlInterpolated($"SELECT * FROM public.\"Wallet\" WHERE \"WalletType\" = {systemWalletType} AND \"Purpose\" = {purposeValue} FOR UPDATE")
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<wallet?> GetByIdAsync(Guid walletId, CancellationToken ct = default)
        {
            var entity = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(x => x.WalletId == walletId, ct);
            return entity?.ToDomain();
        }

        public async Task<List<wallet>> GetAllSystemWalletsAsync(CancellationToken ct = default)
        {
            int systemWalletType = (int)WalletTypeEnum.System;

            var entities = await _db.Wallets
                .AsNoTracking()
                .Where(x => x.WalletType == systemWalletType)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }

    }
}
