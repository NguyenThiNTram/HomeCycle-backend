using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Payments
{
    public interface IPaymentTransactionRepository
    {
        Task<payment_transaction?> GetByPayOSOrderCodeAsync(string payOSOrderCode, CancellationToken ct = default);
        Task AddAsync(payment_transaction transaction, CancellationToken ct = default);
        Task UpdateAsync(payment_transaction transaction, CancellationToken ct = default);
        Task<payment_transaction?> GetLatestByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
        Task<bool> ExistsByPayOSOrderCodeAsync(string payOSOrderCode, CancellationToken ct = default);
    }
}
