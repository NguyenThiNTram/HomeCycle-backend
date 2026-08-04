using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Shipments
{
    public class ShipmentRepository : IShipmentRepository
    {
        private readonly HomeCycleDbContext _db;
        public ShipmentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<shipment?> GetByIdAsync(Guid shipmentId, CancellationToken ct = default)
        {
            var entity = await _db.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, ct);
            return entity?.ToDomain();
        }

        public async Task<shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Shipments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(shipment shipment, CancellationToken ct = default)
        {
            await _db.Shipments.AddAsync(shipment.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(shipment shipment, CancellationToken ct = default)
        {
            _db.Shipments.Update(shipment.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
