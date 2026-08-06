using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Appointments
{
    public class InspectionAppointmentRepository : IInspectionAppointmentRepository
    {
        private readonly HomeCycleDbContext _db;
        public InspectionAppointmentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<inspection_appointment?> GetByIdAsync(Guid inspectionAppointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InspectionAppointmentId == inspectionAppointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task<inspection_appointment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Inspection_Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(inspection_appointment entity, CancellationToken ct = default)
        {
            await _db.Inspection_Appointments.AddAsync(entity.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(inspection_appointment entity, CancellationToken ct = default)
        {
            _db.Inspection_Appointments.Update(entity.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
