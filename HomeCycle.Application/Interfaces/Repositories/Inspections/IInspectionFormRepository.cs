using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Inspections
{
    public interface IInspectionFormRepository
    {
        Task<inspection_form?> GetByIdAsync(Guid inspectionFormId, CancellationToken ct = default);
        Task<inspection_form?> GetByIdForUpdateAsync(Guid inspectionFormId, CancellationToken ct = default);

        Task<inspection_form?> GetByInspectionAppointmentIdAsync(Guid inspectionAppointmentId, CancellationToken ct = default);
        Task<inspection_form?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken ct = default);
        Task<inspection_form?> GetAcceptedCollectNowByOrderIdAsync(Guid orderId, CancellationToken ct = default);

        Task AddAsync(inspection_form form, CancellationToken ct = default);
        Task UpdateAsync(inspection_form form, CancellationToken ct = default);
    }
}
