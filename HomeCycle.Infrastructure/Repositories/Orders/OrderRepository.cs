using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Orders
{
    public class OrderRepository : IOrderRepository
    {
        private readonly HomeCycleDbContext _db;
        public OrderRepository(HomeCycleDbContext db) => _db = db;

        public async Task<order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(order order, CancellationToken ct = default)
        {
            await _db.Orders.AddAsync(order.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(order order, CancellationToken ct = default)
        {
            _db.Orders.Update(order.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
