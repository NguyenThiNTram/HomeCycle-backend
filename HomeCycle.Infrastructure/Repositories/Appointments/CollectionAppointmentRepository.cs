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
    public class CollectionAppointmentRepository : ICollectionAppointmentRepository
    {
        private readonly HomeCycleDbContext _db;
        public CollectionAppointmentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<collection_appointment?> GetByIdAsync(Guid collectionAppointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Collection_Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CollectionAppointmentId == collectionAppointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task<collection_appointment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Collection_Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(collection_appointment entity, CancellationToken ct = default)
        {
            await _db.Collection_Appointments.AddAsync(entity.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(collection_appointment entity, CancellationToken ct = default)
        {
            _db.Collection_Appointments.Update(entity.ToInfrastructure());
            return Task.CompletedTask;
        }
    }
}
