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
        CancellationToken cancellationToken = default)
        {
            // Atomic Update trực tiếp trên bảng GHN_Shipment
            var affectedRows = await _db.GHN_Shipments
                .Where(x =>
                    x.ShipmentId == shipmentId && // Tìm đúng bản ghi dựa trên ShipmentId khóa ngoại
                    x.GHNOrderCode == null &&     // Đơn chưa được tạo thành công trên GHN
                    (
                        x.CreationStatus == (int)GHNCreationStatus.Pending ||
                        x.CreationStatus == (int)GHNCreationStatus.Failed
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
    }
}
