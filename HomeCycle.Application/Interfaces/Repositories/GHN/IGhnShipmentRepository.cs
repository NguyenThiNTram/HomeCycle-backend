using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.GHN
{
    public interface IGhnShipmentRepository
    {
        Task<ghn_shipment?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken);
        Task<ghn_shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
        Task<ghn_shipment?> GetByClientOrderCodeAsync(string clientOrderCode, CancellationToken cancellationToken);
        Task<IReadOnlyList<ghn_shipment>> GetCreationCandidatesAsync(int limit, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default);
        Task AddAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken); // CreationStatus = Pending
        Task UpdateAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken); //Chuyển đổi trạng thái đồng bộ, cập nhật mã lỗi, hoặc lưu OrderCode của GHN
        Task<bool> TryClaimCreationAsync(Guid shipmentId, string newClientOrderCode, DateTime now, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default);
    }
}
