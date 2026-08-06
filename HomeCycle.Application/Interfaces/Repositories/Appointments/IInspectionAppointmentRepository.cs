using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Appointments
{
    public interface IInspectionAppointmentRepository
    {
        Task<inspection_appointment?> GetByIdAsync(Guid inspectionAppointmentId, CancellationToken ct = default);
        Task<inspection_appointment?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
        Task AddAsync(inspection_appointment entity, CancellationToken ct = default);
        Task UpdateAsync(inspection_appointment entity, CancellationToken ct = default);
    }
}
