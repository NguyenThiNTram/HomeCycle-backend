using HomeCycle.Application.Interfaces.Repositories.Inspections;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Inspections
{
    public class InspectionFormRepository : IInspectionFormRepository
    {
        private readonly HomeCycleDbContext _db;

        public InspectionFormRepository(HomeCycleDbContext db) => _db = db;

        public async Task<inspection_form?> GetByIdAsync(Guid inspectionFormId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Forms.AsNoTracking().FirstOrDefaultAsync(x => x.InspectionFormId == inspectionFormId, ct);
            return entity?.ToDomain();
        }

        public async Task<inspection_form?> GetByIdForUpdateAsync(Guid inspectionFormId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Forms
                .FromSqlInterpolated($"SELECT * FROM public.\"Inspection_Form\" WHERE \"InspectionFormId\" = {inspectionFormId} FOR UPDATE")
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<inspection_form?> GetByInspectionAppointmentIdAsync(Guid inspectionAppointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Forms.AsNoTracking()
                .FirstOrDefaultAsync(x => x.InspectionAppointmentId == inspectionAppointmentId, ct);

            return entity?.ToDomain();
        }

        public async Task<inspection_form?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Forms.AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<inspection_form?> GetAcceptedCollectNowByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var action = (int)InspectionCollectAction.CollectNow;

            var entity = await _db.Inspection_Forms.AsNoTracking()
                .Where(x => x.OrderId == orderId &&
                            x.InspectionStatus == (int)InspectionStatus.Accepted &&
                            x.CollectAction == action)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task AddAsync(inspection_form form, CancellationToken ct = default)
        {
            await _db.Inspection_Forms.AddAsync(form.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(inspection_form form, CancellationToken ct = default)
        {
            _db.Inspection_Forms.Update(form.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
