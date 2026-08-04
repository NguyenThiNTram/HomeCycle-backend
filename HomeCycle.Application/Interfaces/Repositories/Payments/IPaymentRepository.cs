using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Payments
{
    public interface IPaymentRepository
    {
        Task<payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default);
        Task AddAsync(payment payment, CancellationToken ct = default);
        Task UpdateAsync(payment payment, CancellationToken ct = default);
    }
}
