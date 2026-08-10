using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Generics;
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
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentService(
            IAppointmentRepository appointmentRepo,
            IInspectionAppointmentRepository inspectionRepo,
            ICollectionAppointmentRepository collectionRepo,
            IAgreementFormRepository agreementRepo,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepo = appointmentRepo;
            _inspectionRepo = inspectionRepo;
            _collectionRepo = collectionRepo;
            _agreementRepo = agreementRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<InspectionAppointmentListItemDto>>> GetInspectionListAsync(
           Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
        {
            var result = await _appointmentRepo.GetPagedInspectionListAsync(userId, isSeller, request, ct);
            return Result<PagedResult<InspectionAppointmentListItemDto>>.Success(result);
        }

        public async Task<Result<PagedResult<CollectionAppointmentListItemDto>>> GetCollectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
        {
            var result = await _appointmentRepo.GetPagedCollectionListAsync(userId, isSeller, request, ct);
            return Result<PagedResult<CollectionAppointmentListItemDto>>.Success(result);
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

        public async Task<Result<AppointmentCheckInResponseDto>> CheckInAsync(
            Guid appointmentId, Guid userId, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, ct);
            if (appointment == null)
                return Result<AppointmentCheckInResponseDto>.Fail(new Error("Appointment.NotFound", "Không tìm thấy lịch hẹn."));

            var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);
            if (agreement == null)
                return Result<AppointmentCheckInResponseDto>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận gắn với lịch hẹn."));

            bool isBuyer = agreement.BuyerId == userId;
            bool isSeller = agreement.SellerId == userId;

            if (!isBuyer && !isSeller)
                return Result<AppointmentCheckInResponseDto>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền check-in lịch hẹn này."));

            // Không cho check-in nếu lịch hẹn đã bị huỷ hoặc đã hoàn tất.
            if (appointment.CancelledAt.HasValue)
                return Result<AppointmentCheckInResponseDto>.Fail(new Error("Appointment.Cancelled", "Lịch hẹn đã bị huỷ, không thể check-in."));

            if (appointment.AppointmentStatus == (int)AppointmentStatus.Completed)
                return Result<AppointmentCheckInResponseDto>.Fail(new Error("Appointment.AlreadyCompleted", "Lịch hẹn đã hoàn tất."));

            var now = DateTime.UtcNow;

            // Idempotent: nếu người này đã check-in rồi thì không ghi đè lại timestamp.
            if (isBuyer && !appointment.BuyerCheckAt.HasValue)
                appointment.BuyerCheckAt = now;

            if (isSeller && !appointment.SellerCheckAt.HasValue)
                appointment.SellerCheckAt = now;

            appointment.UpdatedAt = now;

            // Cả hai bên đã check-in -> tự động Completed.
            if (appointment.BuyerCheckAt.HasValue && appointment.SellerCheckAt.HasValue
                && appointment.AppointmentStatus != (int)AppointmentStatus.Completed)
            {
                appointment.AppointmentStatus = (int)AppointmentStatus.Completed;
                appointment.CompletedAt = now;
            }

            await _appointmentRepo.UpdateAsync(appointment, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<AppointmentCheckInResponseDto>.Success(new AppointmentCheckInResponseDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentStatus = appointment.AppointmentStatus,
                BuyerCheckAt = appointment.BuyerCheckAt,
                SellerCheckAt = appointment.SellerCheckAt
            });
        }
    }
}
