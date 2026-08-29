using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Appointments
{
    public interface IAppointmentService
    {

        Task<Result<PagedResult<InspectionAppointmentListItemDto>>> GetInspectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default);

        Task<Result<PagedResult<CollectionAppointmentListItemDto>>> GetCollectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default);

        Task<Result<AppointmentCheckInResponseDto>> CheckInAsync(
            Guid appointmentId, Guid userId, CancellationToken ct = default);

        Task<Result<AppointmentDetailDto>> GetDetailAsync(Guid appointmentId, Guid userId, CancellationToken ct = default);

        Task<Result<AppointmentRescheduleResponseDto>> RequestRescheduleAsync(Guid appointmentId, Guid userId, RescheduleAppointmentRequest request, CancellationToken ct = default);

        Task<Result<AppointmentRescheduleResponseDto>> AcceptRescheduleAsync(Guid proposalAppointmentId, Guid userId, CancellationToken ct = default);

        Task<Result<AppointmentRescheduleResponseDto>> RejectRescheduleAsync(Guid proposalAppointmentId, Guid userId, RejectAppointmentRescheduleRequest request, CancellationToken ct = default);

        Task<Result<AppointmentActionResponseDto>> CancelAsync(Guid appointmentId, Guid userId, CancelAppointmentRequest request, CancellationToken ct = default);
    }
}
