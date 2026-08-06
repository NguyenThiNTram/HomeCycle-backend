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
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HomeCycleDbContext _db;
        public AppointmentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task<appointment?> GetByAgreementIdAsync(Guid agreementId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.AgreementId == agreementId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(appointment appointment, CancellationToken ct = default)
        {
            await _db.Appointments.AddAsync(appointment.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(appointment appointment, CancellationToken ct = default)
        {
            _db.Appointments.Update(appointment.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
