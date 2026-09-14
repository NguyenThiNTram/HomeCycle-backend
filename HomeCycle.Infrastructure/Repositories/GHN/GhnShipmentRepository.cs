using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using MathNet.Numerics.RootFinding;
using MathNet.Numerics.Statistics.Mcmc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.GHN
{
    public class GhnShipmentRepository : IGhnShipmentRepository
    {
        private readonly HomeCycleDbContext _db;

        public GhnShipmentRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(ghnShipment);

            var infraEntity = ghnShipment.ToInfrastructure();
            await _db.GHN_Shipments.AddAsync(infraEntity, cancellationToken);

        }

        public async Task<ghn_shipment?> GetByClientOrderCodeAsync(string clientOrderCode, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderCode);

            var entity = await _db.GHN_Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ClientOrderCode == clientOrderCode,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> TrySaveCarrierStateAsync(ghn_shipment row, shipment shipment, GhnStateVersion expected, CancellationToken cancellationToken = default)
        {
            var ownsTransaction = _db.Database.CurrentTransaction == null;
            await using var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;
            var changed = await _db.GHN_Shipments.Where(x => x.GHNShipmentId == row.GHNShipmentId &&
                    x.LastSyncedAt == expected.SyncedAt && x.GHNOrderCode == expected.OrderCode &&
                    x.GHNStatusCode == expected.Status && x.CreationStatus == (int)expected.CreationStatus)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.GHNOrderCode, row.GHNOrderCode)
                    .SetProperty(x => x.GHNStatusCode, row.GHNStatusCode)
                    .SetProperty(x => x.CreationStatus, (int)row.CreationStatus)
                    .SetProperty(x => x.LastSyncedAt, row.LastSyncedAt)
                    .SetProperty(x => x.LastErrorCode, row.LastErrorCode)
                    .SetProperty(x => x.ExpectedDeliveryAt, row.ExpectedDeliveryAt), cancellationToken);
            if (changed == 0) return false;
            await _db.Shipments.Where(x => x.ShipmentId == shipment.ShipmentId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ShipmentStatus, (int)shipment.ShipmentStatus)
                    .SetProperty(x => x.PickedUpAt, shipment.PickedUpAt)
                    .SetProperty(x => x.DeliveredAt, shipment.DeliveredAt)
                    .SetProperty(x => x.UpdatedAt, shipment.UpdatedAt), cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return true;
        }
        public async Task<IReadOnlyList<ghn_shipment>> GetAllByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var rows = await _db.GHN_Shipments.AsNoTracking().Where(x => x.Shipment.OrderId == orderId).ToListAsync(cancellationToken);
            return rows.Select(x => x.ToDomain()).ToArray();
        }
        public async Task<ghn_shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        {
            var entity = await(
                from ghnShipment in _db.GHN_Shipments.AsNoTracking()
                join shipment in _db.Shipments.AsNoTracking()
                    on ghnShipment.ShipmentId equals shipment.ShipmentId
                where shipment.OrderId == orderId
                select ghnShipment)
            .FirstOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<ghn_shipment?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken)
        {
            var entity = await _db.GHN_Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ShipmentId == shipmentId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<IReadOnlyList<ghn_shipment>> GetCancellationCandidatesAsync(int limit, CancellationToken cancellationToken = default)
        {

            var rows = await _db.GHN_Shipments.AsNoTracking()
                .Where(x => x.Shipment.Order.OrderStatus == (int)OrderStatus.Cancelled &&
                    x.Shipment.DeliveryMethod == (int)DeliveryMethod.GhnDelivery &&
                    (x.GHNStatusCode == null || x.GHNStatusCode != "cancel") &&
                    (x.LastErrorCode == null || x.LastErrorCode != "CANCEL:REFUSED") &&

                    (x.GHNOrderCode != null || x.Shipment.ShipmentStatus != (int)ShipmentStatus.Cancelled))
                .OrderBy(x => x.LastSyncedAt).Take(limit).ToListAsync(cancellationToken);
            return rows.Select(x => x.ToDomain()).ToArray();
        }

        public async Task SaveCreationFailureAsync(ghn_shipment expected, GHNCreationStatus status, string errorCode, CancellationToken cancellationToken = default)
        {
            await _db.GHN_Shipments.Where(x => x.GHNShipmentId == expected.GHNShipmentId && x.GHNOrderCode == null &&
                    x.CreationStatus == (int)expected.CreationStatus && x.LastCreateAttemptAt == expected.LastCreateAttemptAt)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreationStatus, (int)status)
                    .SetProperty(x => x.LastErrorCode, errorCode)
                    .SetProperty(x => x.LastSyncedAt, DateTime.UtcNow), cancellationToken);
        }
        public async Task SaveCreationResultAsync(Guid shipmentId, HomeCycle.Application.DTOs.Responses.GHN.GhnCreateOrderResponse response, CancellationToken cancellationToken = default)
        {
            // Chỉ cập nhật kết quả create; không ghi đè trạng thái/time của webhook chạy đồng thời.
            var count = await _db.GHN_Shipments.Where(x => x.ShipmentId == shipmentId &&
                (x.GHNOrderCode == null || x.GHNOrderCode == response.OrderCode))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.GHNOrderCode, response.OrderCode)
                    .SetProperty(x => x.CreationStatus, (int)GHNCreationStatus.Success)
                    .SetProperty(x => x.GHNServiceFee, response.ServiceFee)
                    .SetProperty(x => x.GHNCodFee, response.CodFee)
                    .SetProperty(x => x.GHNTotalFee, response.TotalFee)
                    .SetProperty(x => x.LastErrorCode, x => x.LastErrorCode != null && x.LastErrorCode.StartsWith("CANCEL:") ? x.LastErrorCode : null)
                    .SetProperty(x => x.ExpectedDeliveryAt, x => x.ExpectedDeliveryAt ?? (response.ExpectedDeliveryAt.HasValue ? response.ExpectedDeliveryAt.Value.UtcDateTime : (DateTime?)null)), cancellationToken);
            if (count != 1) throw new InvalidOperationException("Mã vận đơn create không khớp shipment hiện có.");
        }
        public async Task<IReadOnlyList<ghn_shipment>> GetCreationCandidatesAsync(
            int limit,
            TimeSpan reclaimProcessingAfter,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

            // Đơn Processing kẹt (worker chết giữa chừng) chỉ được claim lại sau khoảng thời gian chờ.
            if (reclaimProcessingAfter <= TimeSpan.Zero)
                reclaimProcessingAfter = TimeSpan.FromMinutes(5);

            var staleCutoff = DateTime.UtcNow - reclaimProcessingAfter;

            var entities = await _db.GHN_Shipments
                .AsNoTracking()
                .Where(x =>
                    x.GHNOrderCode == null && // đơn chưa từng được GHN tạo thành công
                    x.Shipment.SellerReadyAt.HasValue &&
                    x.Shipment.DeliveryMethod == (int)DeliveryMethod.GhnDelivery &&
                    x.Shipment.ShipmentStatus == (int)ShipmentStatus.ReadyToPick &&
                    x.Shipment.Order.OrderStatus == (int)OrderStatus.Processing &&
                    (
                        x.CreationStatus == (int)GHNCreationStatus.Pending ||
                        (x.CreationStatus == (int)GHNCreationStatus.Failed && (x.LastErrorCode == null || !x.LastErrorCode.StartsWith("PERMANENT:"))) ||
                        (x.CreationStatus == (int)GHNCreationStatus.Uncertain && (x.LastCreateAttemptAt == null || x.LastCreateAttemptAt < staleCutoff)) ||
                        // Claim lại đơn Processing đã mắc kẹt quá lâu (LastCreateAttemptAt cũ)
                        (x.CreationStatus == (int)GHNCreationStatus.Processing &&
                         (x.LastCreateAttemptAt == null || x.LastCreateAttemptAt < staleCutoff))
                    ))
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public Task UpdateAsync(ghn_shipment ghnShipment, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(ghnShipment);
            cancellationToken.ThrowIfCancellationRequested();

            var entity = ghnShipment.ToInfrastructure();

            var trackedEntity = _db.GHN_Shipments.Local.FirstOrDefault(
                x => x.GHNShipmentId == entity.GHNShipmentId);

            if (trackedEntity is not null)
            {
                _db.Entry(trackedEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                _db.GHN_Shipments.Update(entity);
            }

            return Task.CompletedTask;
        }

        public async Task<bool> TryClaimCreationAsync(Guid shipmentId,
        string newClientOrderCode,
        DateTime now,
        TimeSpan reclaimProcessingAfter,
        CancellationToken cancellationToken = default)
        {
            if (reclaimProcessingAfter <= TimeSpan.Zero)
                reclaimProcessingAfter = TimeSpan.FromMinutes(5);

            var staleCutoff = now - reclaimProcessingAfter;

            // Atomic Update trực tiếp trên bảng GHN_Shipment
            var affectedRows = await _db.GHN_Shipments
                .Where(x =>
                    x.ShipmentId == shipmentId && // Tìm đúng bản ghi dựa trên ShipmentId khóa ngoại
                    x.GHNOrderCode == null &&     // Đơn chưa được tạo thành công trên GHN
                    x.Shipment.SellerReadyAt.HasValue &&
                    x.Shipment.DeliveryMethod == (int)DeliveryMethod.GhnDelivery &&
                    x.Shipment.ShipmentStatus == (int)ShipmentStatus.ReadyToPick &&
                    x.Shipment.Order.OrderStatus == (int)OrderStatus.Processing &&
                    (
                        x.CreationStatus == (int)GHNCreationStatus.Pending ||
                        (x.CreationStatus == (int)GHNCreationStatus.Failed && (x.LastErrorCode == null || !x.LastErrorCode.StartsWith("PERMANENT:"))) ||
                        (x.CreationStatus == (int)GHNCreationStatus.Uncertain && (x.LastCreateAttemptAt == null || x.LastCreateAttemptAt < staleCutoff)) ||
                        // Claim lại đơn Processing mắc kẹt (LastCreateAttemptAt quá cũ)
                        (x.CreationStatus == (int)GHNCreationStatus.Processing &&
                         (x.LastCreateAttemptAt == null || x.LastCreateAttemptAt < staleCutoff))
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.CreationStatus, (int)GHNCreationStatus.Processing)
                    .SetProperty(x => x.LastCreateAttemptAt, now)
                    .SetProperty(x => x.LastErrorCode, (string?)null) // Xóa mã lỗi cũ của lượt trước

                    // Nếu là đơn mới (chưa có ClientOrderCode) -> gán mã mới
                    // Nếu đã có mã (đơn Failed lượt trước), giữ nguyên mã cũ để kích hoạt tính năng chống trùng đơn (Idempotency) phía GHN
                    .SetProperty(x => x.ClientOrderCode, x => string.IsNullOrEmpty(x.ClientOrderCode) ? newClientOrderCode : x.ClientOrderCode),
                    cancellationToken);

            if (affectedRows == 1)
            {
                await _db.Shipments
                    .Where(s => s.ShipmentId == shipmentId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.UpdatedAt, now), cancellationToken);
            }

            return affectedRows == 1;
        }

        public async Task<ghn_shipment?> GetByGhnOrderCodeAsync(string ghnOrderCode, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ghnOrderCode);

            var normalizedOrderCode = ghnOrderCode.Trim();

            var entity = await _db.GHN_Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.GHNOrderCode == normalizedOrderCode,
                    cancellationToken);

            return entity?.ToDomain();
        }
    }
}
