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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly HomeCycleDbContext _db;
        public PaymentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default)
        {
            var entity = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(payment payment, CancellationToken ct = default)
        {
            await _db.Payments.AddAsync(payment.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(payment payment, CancellationToken ct = default)
        {
            _db.Payments.Update(payment.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
