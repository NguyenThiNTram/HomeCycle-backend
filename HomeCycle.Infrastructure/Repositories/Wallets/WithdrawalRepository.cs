using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
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
    public class WithdrawalRepository : IWithdrawalRepository
    {
        private readonly HomeCycleDbContext _db;
        public WithdrawalRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(withdrawal withdrawal, CancellationToken ct = default)
            => await _db.Withdrawals.AddAsync(withdrawal.ToInfrastructure(), ct);

        public async Task<withdrawal?> GetByIdAsync(Guid withdrawalId, CancellationToken ct = default)
        {
            var entity = await _db.Withdrawals.FirstOrDefaultAsync(x => x.WithdrawalId == withdrawalId, ct);
            return entity?.ToDomain();
        }

        public Task UpdateAsync(withdrawal withdrawal, CancellationToken ct = default)
        {
            var entity = withdrawal.ToInfrastructure();
            var local = _db.Withdrawals.Local.FirstOrDefault(x => x.WithdrawalId == entity.WithdrawalId);
            if (local != null) _db.Entry(local).State = EntityState.Detached;
            _db.Withdrawals.Update(entity);
            return Task.CompletedTask;
        }
    }
}
