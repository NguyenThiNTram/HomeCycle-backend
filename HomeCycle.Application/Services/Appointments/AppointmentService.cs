using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IInspectionAppointmentRepository _inspectionRepo;
        private readonly ICollectionAppointmentRepository _collectionRepo;
        private readonly IAgreementFormRepository _agreementRepo;

        public AppointmentService(
            IAppointmentRepository appointmentRepo,
            IInspectionAppointmentRepository inspectionRepo,
            ICollectionAppointmentRepository collectionRepo,
            IAgreementFormRepository agreementRepo)
        {
            _appointmentRepo = appointmentRepo;
            _inspectionRepo = inspectionRepo;
            _collectionRepo = collectionRepo;
            _agreementRepo = agreementRepo;
        }

        public Task<Result<PagedResult<appointment>>> GetInspectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
            => GetByTypeAsync(AppointmentType.Inspection, userId, isSeller, request, ct);

        public Task<Result<PagedResult<appointment>>> GetCollectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
            => GetByTypeAsync(AppointmentType.Collection, userId, isSeller, request, ct);

        private async Task<Result<PagedResult<appointment>>> GetByTypeAsync(
            AppointmentType type, Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct)
        {
            var result = await _appointmentRepo.GetPagedByTypeAsync(type, userId, isSeller, request, ct);
            return Result<PagedResult<appointment>>.Success(result);
        }

        public async Task<Result<AppointmentDetailDto>> GetDetailAsync(Guid appointmentId, Guid userId, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, ct);
            if (appointment == null)
                return Result<AppointmentDetailDto>.Fail(new Error("Appointment.NotFound", "Không tìm thấy lịch hẹn."));

            var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);
            if (agreement == null)
                return Result<AppointmentDetailDto>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận gắn với lịch hẹn."));

            // Chỉ buyer hoặc seller của Agreement này mới được xem.
            if (agreement.BuyerId != userId && agreement.SellerId != userId)
                return Result<AppointmentDetailDto>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền xem lịch hẹn này."));

            var dto = new AppointmentDetailDto { Appointment = appointment };

            if (appointment.AppointmentType == (int)AppointmentType.Inspection)
                dto.InspectionAppointment = await _inspectionRepo.GetByAppointmentIdAsync(appointmentId, ct);
            else
                dto.CollectionAppointment = await _collectionRepo.GetByAppointmentIdAsync(appointmentId, ct);

            return Result<AppointmentDetailDto>.Success(dto);
        }
    }
}
