using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Orders
{
    public interface IOrderRepository
    {
        Task<order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
        Task AddAsync(order order, CancellationToken ct = default);
        Task UpdateAsync(order order, CancellationToken ct = default);
    }
}
