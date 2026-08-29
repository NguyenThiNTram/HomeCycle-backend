using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        private readonly IOrderRepository _orderRepo;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository appointmentRepo,
            IInspectionAppointmentRepository inspectionRepo,
            ICollectionAppointmentRepository collectionRepo,
            IAgreementFormRepository agreementRepo,
            IOrderRepository orderRepo,
            IPlatformPolicyProvider platformPolicyProvider,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _appointmentRepo = appointmentRepo;
            _inspectionRepo = inspectionRepo;
            _collectionRepo = collectionRepo;
            _agreementRepo = agreementRepo;
            _orderRepo = orderRepo;
            _platformPolicyProvider = platformPolicyProvider;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                return Result<AppointmentDetailDto>.Fail(AppointmentErrors.NotFound);

            var agreement = await _agreementRepo.GetByIdAsync(
                appointment.AgreementId,
                ct);

            if (agreement == null)
                return Result<AppointmentDetailDto>.Fail(AgreementErrors.NotFound);

            var isBuyer = agreement.BuyerId == userId;
            var isSeller = agreement.SellerId == userId;

            if (!isBuyer && !isSeller)
                return Result<AppointmentDetailDto>.Fail(AppointmentErrors.Forbidden);

            var dto = _mapper.Map<AppointmentDetailDto>(appointment);

            DateTime? scheduledAt = null;

            if (appointment.AppointmentType == (int)AppointmentType.Inspection)
            {
                var inspection = await _inspectionRepo.GetByAppointmentIdAsync(
                    appointmentId,
                    ct);

                if (inspection == null)
                    return Result<AppointmentDetailDto>.Fail(
                        AppointmentErrors.InspectionDetailNotFound);

                dto.Inspection =
                    _mapper.Map<InspectionAppointmentDetailDto>(inspection);

                scheduledAt = inspection.InspectionDate;
            }
            else if (appointment.AppointmentType == (int)AppointmentType.Collection)
            {
                var collection = await _collectionRepo.GetByAppointmentIdAsync(
                    appointmentId,
                    ct);

                if (collection == null)
                    return Result<AppointmentDetailDto>.Fail(
                        AppointmentErrors.CollectionDetailNotFound);

                dto.Collection =
                    _mapper.Map<CollectionAppointmentDetailDto>(collection);

                scheduledAt = collection.CollectionDate;

                if (!string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                {
                    try
                    {
                        var details =
                            JsonSerializer.Deserialize<AgreementDetailsDto>(
                                agreement.AgreementDetailsJsonb,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                        dto.Collection.DeliveryMethod =
                            details?.DeliveryMethod;
                    }
                    catch (JsonException)
                    {
                        dto.Collection.DeliveryMethod = null;
                    }
                }
            }
            else
            {
                return Result<AppointmentDetailDto>.Fail(
                    AppointmentErrors.InvalidType);
            }

            if (appointment.CancelledAt.HasValue)
            {
                dto.Cancellation = new AppointmentCancellationDto
                {
                    CancelledAt = appointment.CancelledAt.Value,
                    Reason = appointment.CancellationReason
                };
            }

            var order = await _orderRepo.GetByAgreementIdAsync(
                appointment.AgreementId,
                ct);

            if (order == null)
                return Result<AppointmentDetailDto>.Fail(OrderErrors.NotFound);

            dto.Order =
                _mapper.Map<AppointmentOrderSummaryDto>(order);

            var policy =
                await _platformPolicyProvider.GetAppointmentConfigAsync(ct);

            var status = appointment.AppointmentStatus.HasValue
                ? (AppointmentStatus?)appointment.AppointmentStatus.Value
                : null;

            var isTerminal =
                appointment.CancelledAt.HasValue ||
                appointment.CompletedAt.HasValue ||
                status == AppointmentStatus.Cancelled ||
                status == AppointmentStatus.Completed ||
                status == AppointmentStatus.Misssed;

            var eligibleStatus =
                status == AppointmentStatus.Pending ||
                status == AppointmentStatus.Confirmed;

            var currentUserCheckedIn = isBuyer
                ? appointment.BuyerCheckAt.HasValue
                : appointment.SellerCheckAt.HasValue;

            var hasAnyCheckIn =
                appointment.BuyerCheckAt.HasValue ||
                appointment.SellerCheckAt.HasValue;

            var supportsUserAppointmentActions =
                appointment.AppointmentType == (int)AppointmentType.Inspection ||
                (
                    appointment.AppointmentType == (int)AppointmentType.Collection &&
                    (
                        dto.Collection?.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                        dto.Collection?.DeliveryMethod == DeliveryMethod.SellerDelivers
                    )
                );

            var canCheckIn = false;
            var canRequestReschedule = false;
            var canCancel = false;

            if (scheduledAt.HasValue &&
                eligibleStatus &&
                !isTerminal &&
                supportsUserAppointmentActions)
            {
                var now = DateTime.UtcNow;

                var checkInOpenAt =
                    scheduledAt.Value.AddMinutes(
                        -policy.CheckInOpenBeforeMinutes);

                var checkInCloseAt =
                    scheduledAt.Value.AddMinutes(
                        policy.NoInteractionExpiryMinutes);

                canCheckIn =
                    !currentUserCheckedIn &&
                    now >= checkInOpenAt &&
                    now <= checkInCloseAt;

                canRequestReschedule =
                    !hasAnyCheckIn &&
                    now <= scheduledAt.Value.AddHours(
                        -policy.RescheduleCutoffHours);

                canCancel =
                    !hasAnyCheckIn &&
                    now <= scheduledAt.Value.AddHours(
                        -policy.CancellationCutoffHours);
            }

            dto.Actions = new AppointmentActionDto
            {
                CanCheckIn = canCheckIn,
                CanRequestReschedule = canRequestReschedule,
                CanCancel = canCancel
            };

            return Result<AppointmentDetailDto>.Success(dto);
        }

        public async Task<Result<AppointmentCheckInResponseDto>> CheckInAsync(
            Guid appointmentId, Guid userId, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, ct);
            if (appointment == null)
                return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.NotFound);

            var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);
            if (agreement == null)
                return Result<AppointmentCheckInResponseDto>.Fail(AgreementErrors.NotFound);

            bool isBuyer = agreement.BuyerId == userId;
            bool isSeller = agreement.SellerId == userId;

            if (!isBuyer && !isSeller)
                return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.Forbidden);

            // Không cho check-in nếu lịch hẹn đã bị huỷ hoặc đã hoàn tất.
            if (appointment.CancelledAt.HasValue)
                return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.Cancelled);

            if (appointment.AppointmentStatus == (int)AppointmentStatus.Completed)
                return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.AlreadyCompleted);

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
