using HomeCycle.Application.Interfaces.Repositories.Payments;
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

        public async Task<payment_transaction?> GetByPayOSOrderCodeForUpdateAsync(
            string payOSOrderCode,
            CancellationToken ct = default)
        {
            EnsureActiveTransaction();

            var entity = await _db.Payment_Transactions
                .FromSqlInterpolated($@"
            SELECT *
            FROM ""Payment_Transaction""
            WHERE ""PayOSOrderCode"" = {payOSOrderCode}
            FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<IReadOnlyList<payment_transaction>> GetPendingPayOsSyncCandidatesAsync(
            int limit,
            TimeSpan retryAfter,
            CancellationToken ct = default)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            if (retryAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryAfter));

            var staleCutoff = DateTime.UtcNow - retryAfter;

            var entities = await _db.Payment_Transactions
                .AsNoTracking()
                .Where(x =>
                    x.PaymentTransactionStatus == (int)PaymentTransactionStatus.Pending &&
                    x.PayOSOrderCode != null &&
                    x.PayOSOrderCode != string.Empty &&
                    x.UpdatedAt <= staleCutoff &&
                    x.Payment.PaymentStatus == (int)PaymentStatus.Pending &&
                    x.Payment.PaymentMethod == (int)PaymentMethod.PayOS &&
                    x.Payment.AgreementId.HasValue)
                .OrderBy(x => x.UpdatedAt)
                .ThenBy(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);

            return entities
                .Select(x => x.ToDomain())
                .ToList();
        }

        public async Task<bool> TryClaimPayOsSyncAsync(
            Guid paymentTransactionId,
            DateTime now,
            TimeSpan retryAfter,
            CancellationToken ct = default)
        {
            if (retryAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryAfter));

            var staleCutoff = now - retryAfter;

            var affectedRows = await _db.Payment_Transactions
                .Where(x =>
                    x.PaymentTransactionId == paymentTransactionId &&
                    x.PaymentTransactionStatus == (int)PaymentTransactionStatus.Pending &&
                    x.PayOSOrderCode != null &&
                    x.PayOSOrderCode != string.Empty &&
                    x.UpdatedAt <= staleCutoff &&
                    x.Payment.PaymentStatus == (int)PaymentStatus.Pending &&
                    x.Payment.PaymentMethod == (int)PaymentMethod.PayOS &&
                    x.Payment.AgreementId.HasValue)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.UpdatedAt, now),
                    ct);

            return affectedRows == 1;
        }

        private void EnsureActiveTransaction()
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException(
                    "FOR UPDATE requires an active database transaction.");
        }
    }
}
