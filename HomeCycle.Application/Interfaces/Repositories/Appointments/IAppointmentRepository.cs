using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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
        Task<PagedResult<InspectionAppointmentListItemDto>> GetPagedInspectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default);

        Task<PagedResult<CollectionAppointmentListItemDto>> GetPagedCollectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default);
        Task<appointment?> GetByAgreementIdAndTypeAsync(Guid agreementId, AppointmentType appointmentType, CancellationToken ct = default);

        Task<IReadOnlyList<AppointmentSummaryDto>> GetAppointmentSummariesByAgreementIdAsync(Guid agreementId, CancellationToken ct = default);
    }
}
