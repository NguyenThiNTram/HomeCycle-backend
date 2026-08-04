using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Appointments
{
    public interface ICollectionAppointmentRepository
    {
        Task<collection_appointment?> GetByIdAsync(Guid collectionAppointmentId, CancellationToken ct = default);
        Task<collection_appointment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
        Task AddAsync(collection_appointment entity, CancellationToken ct = default);
        Task UpdateAsync(collection_appointment entity, CancellationToken ct = default);
    }
}
