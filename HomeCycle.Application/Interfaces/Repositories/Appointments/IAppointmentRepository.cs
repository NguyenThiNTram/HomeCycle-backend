using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Appointments
{
    public interface IAppointmentRepository
    {
        Task<appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct = default);
        Task<appointment?> GetByAgreementIdAsync(Guid agreementId, CancellationToken ct = default);
        Task AddAsync(appointment appointment, CancellationToken ct = default);
        Task UpdateAsync(appointment appointment, CancellationToken ct = default);
    }
}
