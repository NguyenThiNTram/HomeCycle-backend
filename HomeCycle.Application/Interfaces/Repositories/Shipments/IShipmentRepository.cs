using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Shipments
{
    public interface IShipmentRepository
    {
        Task<shipment?> GetByIdAsync(Guid shipmentId, CancellationToken ct = default);
        Task<shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
        Task AddAsync(shipment shipment, CancellationToken ct = default);
        Task UpdateAsync(shipment shipment, CancellationToken ct = default);
    }
}
