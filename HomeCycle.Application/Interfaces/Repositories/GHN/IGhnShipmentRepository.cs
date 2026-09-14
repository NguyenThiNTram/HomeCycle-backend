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
        Task<bool> TrySaveCarrierStateAsync(ghn_shipment row, shipment shipment, GhnStateVersion expected, CancellationToken cancellationToken = default);
        Task SaveCreationFailureAsync(ghn_shipment expected, HomeCycle.Domain.Enums.GHNCreationStatus status, string errorCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ghn_shipment>> GetAllByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ghn_shipment>> GetCancellationCandidatesAsync(int limit, CancellationToken cancellationToken = default);
        Task SaveCreationResultAsync(Guid shipmentId, HomeCycle.Application.DTOs.Responses.GHN.GhnCreateOrderResponse response, CancellationToken cancellationToken = default);
        Task<ghn_shipment?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken);
        Task<ghn_shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
        Task<ghn_shipment?> GetByClientOrderCodeAsync(string clientOrderCode, CancellationToken cancellationToken);
        Task<IReadOnlyList<ghn_shipment>> GetCreationCandidatesAsync(int limit, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default);
        Task AddAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken); // CreationStatus = Pending
        Task UpdateAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken); //Chuyển đổi trạng thái đồng bộ, cập nhật mã lỗi, hoặc lưu OrderCode của GHN
        Task<bool> TryClaimCreationAsync(Guid shipmentId, string newClientOrderCode, DateTime now, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default);
        Task<ghn_shipment?> GetByGhnOrderCodeAsync(string ghnOrderCode, CancellationToken cancellationToken);
    }
    public sealed record GhnStateVersion(DateTime? SyncedAt, string? OrderCode, string? Status, HomeCycle.Domain.Enums.GHNCreationStatus CreationStatus)
    {
        public static GhnStateVersion Capture(ghn_shipment row) => new(row.LastSyncedAt, row.GHNOrderCode, row.GHNStatusCode, row.CreationStatus);
    }
}
