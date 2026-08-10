using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Payments
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly HomeCycleDbContext _db;
        public PaymentTransactionRepository(HomeCycleDbContext db) => _db = db;

        public async Task<payment_transaction?> GetByPayOSOrderCodeAsync(string payOSOrderCode, CancellationToken ct = default)
        {
            var entity = await _db.Payment_Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PayOSOrderCode == payOSOrderCode, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(payment_transaction transaction, CancellationToken ct = default)
        {
            var entity = transaction.ToInfrastructure();
            await _db.Payment_Transactions.AddAsync(entity, ct);
        }

        public Task UpdateAsync(payment_transaction transaction, CancellationToken ct = default)
        {
            var entity = transaction.ToInfrastructure();
            _db.Payment_Transactions.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<payment_transaction?> GetLatestByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        {
            var entity = await _db.Payment_Transactions
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByPayOSOrderCodeAsync(string payOSOrderCode, CancellationToken ct = default)
        {
            return await _db.Payment_Transactions
                .AsNoTracking()
                .AnyAsync(x => x.PayOSOrderCode == payOSOrderCode, ct);
        }
    }
}
