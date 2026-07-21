using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Banks
{
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly HomeCycleDbContext _db;

        public BankAccountRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public Task AddAsync(bank_account bankAccount, CancellationToken cancellationToken = default)
        {
            var entity = bankAccount.ToInfrastructure();
            _db.Bank_Accounts.Add(entity);
            return _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bank_account?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Bank_Accounts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task UpdateAsync(bank_account account, CancellationToken cancellationToken = default)
        {
            var entity = account.ToInfrastructure();
            _db.Bank_Accounts.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
