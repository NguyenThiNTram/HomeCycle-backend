using AutoMapper;
using FluentValidation;
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

        private readonly IValidator<RescheduleAppointmentRequest> _rescheduleValidator;
        private readonly IValidator<RejectAppointmentRescheduleRequest> _rejectRescheduleValidator;
        private readonly IValidator<CancelAppointmentRequest> _cancelValidator;

        public AppointmentService(
            IAppointmentRepository appointmentRepo,
            IInspectionAppointmentRepository inspectionRepo,
            ICollectionAppointmentRepository collectionRepo,
            IAgreementFormRepository agreementRepo,
            IOrderRepository orderRepo,
            IPlatformPolicyProvider platformPolicyProvider,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<RescheduleAppointmentRequest> rescheduleValidator,
            IValidator<RejectAppointmentRescheduleRequest> rejectRescheduleValidator,
            IValidator<CancelAppointmentRequest> cancelValidator)
        {
            _appointmentRepo = appointmentRepo;
            _inspectionRepo = inspectionRepo;
            _collectionRepo = collectionRepo;
            _agreementRepo = agreementRepo;
            _orderRepo = orderRepo;
            _platformPolicyProvider = platformPolicyProvider;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _rescheduleValidator = rescheduleValidator;
            _rejectRescheduleValidator = rejectRescheduleValidator;
            _cancelValidator = cancelValidator;
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

            var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);
            if (agreement == null)
                return Result<AppointmentDetailDto>.Fail(AgreementErrors.NotFound);

            var isBuyer = agreement.BuyerId == userId;
            var isSeller = agreement.SellerId == userId;

            if (!isBuyer && !isSeller)
                return Result<AppointmentDetailDto>.Fail(AppointmentErrors.Forbidden);

            var scheduleResult = await GetScheduleContextAsync(appointment, agreement, ct);
            if (!scheduleResult.IsSuccess)
                return Result<AppointmentDetailDto>.Fail(scheduleResult.Error!);

            var schedule = scheduleResult.Data!;
            var dto = _mapper.Map<AppointmentDetailDto>(appointment);

            var status = appointment.AppointmentStatus.HasValue ? (AppointmentStatus?)appointment.AppointmentStatus.Value : null;
            var now = DateTime.UtcNow;
            var policy = await _platformPolicyProvider.GetAppointmentConfigAsync(ct);

            var supportsUserActions = SupportsUserAppointmentActions(schedule);
            var supportsLateThreshold = SupportsLateThreshold(schedule);

            DateTime? lateThresholdAt = null;

            if (supportsLateThreshold)
                lateThresholdAt = appointment.LateThresholdAt ?? schedule.ScheduledAt.AddMinutes(policy.LateThresholdMinutes);

            var isActive = status == AppointmentStatus.Scheduled || status == AppointmentStatus.InProgress;
            var fullyCheckedIn = appointment.BuyerCheckAt.HasValue && appointment.SellerCheckAt.HasValue;

            dto.LateThresholdAt = lateThresholdAt;

            dto.IsOverdue =
                isActive &&
                lateThresholdAt.HasValue &&
                now > lateThresholdAt.Value &&
                (
                    schedule.AppointmentType == AppointmentType.Collection ||
                    !fullyCheckedIn
                );

            var hasAnyCheckIn =
                schedule.AppointmentType == AppointmentType.Inspection &&
                (appointment.BuyerCheckAt.HasValue || appointment.SellerCheckAt.HasValue);

            if (schedule.AppointmentType == AppointmentType.Inspection)
            {
                dto.Inspection = _mapper.Map<InspectionAppointmentDetailDto>(schedule.Inspection);

                var checkInOpenAt = schedule.ScheduledAt.AddMinutes(-policy.CheckInOpenBeforeMinutes);
                var currentUserCheckedIn = isBuyer ? appointment.BuyerCheckAt.HasValue : appointment.SellerCheckAt.HasValue;

                dto.Inspection.CheckIn = new InspectionCheckInDto
                {
                    BuyerCheckAt = appointment.BuyerCheckAt,
                    SellerCheckAt = appointment.SellerCheckAt,
                    CheckInOpenAt = checkInOpenAt,
                    CanCheckIn = isActive && !currentUserCheckedIn && now >= checkInOpenAt
                };
            }
            else
            {
                dto.Collection = _mapper.Map<CollectionAppointmentDetailDto>(schedule.Collection);
                dto.Collection.DeliveryMethod = schedule.DeliveryMethod;
            }

            if (appointment.CancelledAt.HasValue)
            {
                dto.Cancellation = new AppointmentCancellationDto
                {
                    CancelledAt = appointment.CancelledAt.Value,
                    Reason = appointment.CancellationReason
                };
            }

            var order = await _orderRepo.GetByAgreementIdAsync(appointment.AgreementId, ct);
            if (order == null)
                return Result<AppointmentDetailDto>.Fail(OrderErrors.NotFound);

            dto.Order = _mapper.Map<AppointmentOrderSummaryDto>(order);

            var pendingProposal = status == AppointmentStatus.Proposed
                ? null
                : await _appointmentRepo.GetPendingRescheduleProposalAsync(appointment.AppointmentId, ct);

            DateTime? proposedAt = null;
            var isCurrentUserRequester = false;

            if (pendingProposal != null)
            {
                var proposalScheduleResult = await GetScheduleContextAsync(pendingProposal, agreement, ct);

                if (proposalScheduleResult.IsSuccess)
                    proposedAt = proposalScheduleResult.Data!.ScheduledAt;

                isCurrentUserRequester = pendingProposal.RescheduleRequestedByUserId == userId;

                dto.Reschedule = new AppointmentRescheduleInfoDto
                {
                    OriginalAppointmentId = appointment.AppointmentId,
                    ProposalAppointmentId = pendingProposal.AppointmentId,
                    RequestedByUserId = pendingProposal.RescheduleRequestedByUserId,
                    RequestedAt = pendingProposal.RescheduleRequestedAt,
                    ProposedAt = proposedAt,
                    IsCurrentUserRequester = isCurrentUserRequester
                };
            }

            var canRequestReschedule =
                supportsUserActions &&
                status == AppointmentStatus.Scheduled &&
                !hasAnyCheckIn &&
                pendingProposal == null &&
                now <= schedule.ScheduledAt.AddHours(-policy.RescheduleCutoffHours);

            var canCancel =
                supportsUserActions &&
                status == AppointmentStatus.Scheduled &&
                !hasAnyCheckIn &&
                now <= schedule.ScheduledAt.AddHours(-policy.CancellationCutoffHours);

            var canAcceptReschedule = false;
            var canRejectReschedule = false;

            if (pendingProposal != null && !isCurrentUserRequester && status == AppointmentStatus.Scheduled && !hasAnyCheckIn)
            {
                canAcceptReschedule = proposedAt.HasValue && proposedAt.Value > now;
                canRejectReschedule = true;
            }

            dto.Actions = new AppointmentActionDto
            {
                CanRequestReschedule = canRequestReschedule,
                CanAcceptReschedule = canAcceptReschedule,
                CanRejectReschedule = canRejectReschedule,
                CanCancel = canCancel
            };

            return Result<AppointmentDetailDto>.Success(dto);
        }

        public async Task<Result<AppointmentCheckInResponseDto>> CheckInAsync(Guid appointmentId, Guid userId, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var appointment = await _appointmentRepo.GetByIdForUpdateAsync(appointmentId, ct);

                if (appointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AgreementErrors.NotFound);
                }

                var isBuyer = agreement.BuyerId == userId;
                var isSeller = agreement.SellerId == userId;

                if (!isBuyer && !isSeller)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.Forbidden);
                }

                if (appointment.AppointmentType != (int)AppointmentType.Inspection)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.CheckInInspectionOnly);
                }

                var status = appointment.AppointmentStatus.HasValue ? (AppointmentStatus?)appointment.AppointmentStatus.Value : null;

                if (status != AppointmentStatus.Scheduled && status != AppointmentStatus.InProgress)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.InvalidStatus);
                }

                var scheduleResult = await GetScheduleContextAsync(appointment, agreement, ct);

                if (!scheduleResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(scheduleResult.Error!);
                }

                var schedule = scheduleResult.Data!;
                var policy = await _platformPolicyProvider.GetAppointmentConfigAsync(ct);

                var lateThresholdAt = appointment.LateThresholdAt ?? schedule.ScheduledAt.AddMinutes(policy.LateThresholdMinutes);
                var checkInOpenAt = schedule.ScheduledAt.AddMinutes(-policy.CheckInOpenBeforeMinutes);
                var now = DateTime.UtcNow;

                if ((isBuyer && appointment.BuyerCheckAt.HasValue) ||
                    (isSeller && appointment.SellerCheckAt.HasValue))
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    var fullyCheckedIn = appointment.BuyerCheckAt.HasValue && appointment.SellerCheckAt.HasValue;

                    return Result<AppointmentCheckInResponseDto>.Success(new AppointmentCheckInResponseDto
                    {
                        AppointmentId = appointment.AppointmentId,
                        AppointmentStatus = status,
                        BuyerCheckAt = appointment.BuyerCheckAt,
                        SellerCheckAt = appointment.SellerCheckAt,
                        LateThresholdAt = lateThresholdAt,
                        IsOverdue = now > lateThresholdAt && !fullyCheckedIn
                    });
                }

                if (now < checkInOpenAt)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentCheckInResponseDto>.Fail(AppointmentErrors.CheckInNotOpen(checkInOpenAt));
                }

                // QUAN TRỌNG:
                // Không còn check "now > LateThresholdAt => Expired".
                // Quá 2h vẫn được check-in; timestamp được giữ làm evidence nếu có dispute.

                appointment.LateThresholdAt ??= lateThresholdAt;

                var pendingProposal = await _appointmentRepo.GetPendingRescheduleProposalAsync(appointment.AppointmentId, ct);

                if (pendingProposal != null)
                {
                    var lockedProposal = await _appointmentRepo.GetByIdForUpdateAsync(pendingProposal.AppointmentId, ct);

                    if (lockedProposal?.AppointmentStatus == (int)AppointmentStatus.Proposed)
                    {
                        lockedProposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                        lockedProposal.CancelledAt = now;
                        lockedProposal.CancellationReason = "Reschedule request invalidated because appointment started.";
                        lockedProposal.UpdatedAt = now;

                        await _appointmentRepo.UpdateAsync(lockedProposal, ct);
                    }
                }

                if (isBuyer)
                    appointment.BuyerCheckAt = now;
                else
                    appointment.SellerCheckAt = now;

                if (appointment.AppointmentStatus == (int)AppointmentStatus.Scheduled)
                    appointment.AppointmentStatus = (int)AppointmentStatus.InProgress;

                appointment.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(appointment, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                var isFullyCheckedIn = appointment.BuyerCheckAt.HasValue && appointment.SellerCheckAt.HasValue;

                return Result<AppointmentCheckInResponseDto>.Success(new AppointmentCheckInResponseDto
                {
                    AppointmentId = appointment.AppointmentId,
                    AppointmentStatus = (AppointmentStatus)appointment.AppointmentStatus.Value,
                    BuyerCheckAt = appointment.BuyerCheckAt,
                    SellerCheckAt = appointment.SellerCheckAt,
                    LateThresholdAt = appointment.LateThresholdAt,
                    IsOverdue = now > lateThresholdAt && !isFullyCheckedIn
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<AppointmentRescheduleResponseDto>> RequestRescheduleAsync(Guid appointmentId, Guid userId, RescheduleAppointmentRequest request, CancellationToken ct = default)
        {
            var validation = await _rescheduleValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
            {
                var message = string.Join(
                    " | ",
                    validation.Errors.Select(x => x.ErrorMessage));

                return Result<AppointmentRescheduleResponseDto>.Fail(
                    new Error("Validation.InvalidRequest", message));
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var original = await _appointmentRepo.GetByIdForUpdateAsync(appointmentId, ct);

                if (original == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(original.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != userId &&
                    agreement.SellerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.Forbidden);
                }

                if (original.AppointmentStatus !=
                        (int)AppointmentStatus.Scheduled || original.CancelledAt.HasValue || original.CompletedAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.InvalidStatus);
                }

                if (original.AppointmentType == (int)AppointmentType.Inspection &&
                    (original.BuyerCheckAt.HasValue || original.SellerCheckAt.HasValue))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.CheckInAlreadyStarted);
                }

                var scheduleResult =
                    await GetScheduleContextAsync(original, agreement, ct);

                if (!scheduleResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        scheduleResult.Error!);
                }

                var schedule = scheduleResult.Data!;

                if (!SupportsUserAppointmentActions(schedule))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.UnsupportedAction);
                }

                var policy = await _platformPolicyProvider.GetAppointmentConfigAsync(ct);

                var now = DateTime.UtcNow;

                var cutoff = schedule.ScheduledAt.AddHours(-policy.RescheduleCutoffHours);

                if (now > cutoff)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.RescheduleCutoffPassed(cutoff));
                }

                var proposedAt = request.ProposedAt.UtcDateTime;

                if (proposedAt <= now)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.RescheduleProposalExpired);
                }

                if (proposedAt == schedule.ScheduledAt)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.SameSchedule);
                }

                var existingProposal =
                    await _appointmentRepo.GetPendingRescheduleProposalAsync(
                        original.AppointmentId,
                        ct);

                if (existingProposal != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.PendingRescheduleExists);
                }

                var proposalId = Guid.NewGuid();

                var proposal = new appointment
                {
                    AppointmentId = proposalId,
                    AgreementId = original.AgreementId,

                    AppointmentType = original.AppointmentType,
                    AppointmentStatus =
                        (int)AppointmentStatus.Proposed,

                    LateThresholdAt = proposedAt.AddMinutes(policy.LateThresholdMinutes),

                    RescheduledFromAppointmentId = original.AppointmentId,

                    RescheduleRequestedByUserId = userId,
                    RescheduleRequestedAt = now,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _appointmentRepo.AddAsync(proposal, ct);

                if (schedule.AppointmentType == AppointmentType.Inspection)
                {
                    await _inspectionRepo.AddAsync(
                        new inspection_appointment
                        {
                            InspectionAppointmentId = Guid.NewGuid(),

                            AppointmentId = proposalId,

                            InspectionDate = proposedAt,

                            InspectionAddress = schedule.Inspection!.InspectionAddress
                        },
                        ct);
                }
                else
                {
                    await _collectionRepo.AddAsync(
                        new collection_appointment
                        {
                            CollectionAppointmentId = Guid.NewGuid(),

                            AppointmentId = proposalId,

                            CollectionDate = proposedAt,

                            PickupAddress = schedule.Collection!.PickupAddress,

                            DeliveryAddress = schedule.Collection.DeliveryAddress,

                            DeliveryMethod = schedule.Collection.DeliveryMethod
                        },
                        ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<AppointmentRescheduleResponseDto>.Success(
                    new AppointmentRescheduleResponseDto
                    {
                        OriginalAppointmentId = original.AppointmentId,

                        ProposalAppointmentId = proposal.AppointmentId,

                        OriginalStatus = AppointmentStatus.Scheduled,

                        ProposalStatus = AppointmentStatus.Proposed,

                        ProposedAt = proposedAt,

                        RequestedByUserId = userId,
                        RequestedAt = now
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<AppointmentRescheduleResponseDto>> AcceptRescheduleAsync(Guid proposalAppointmentId, Guid userId, CancellationToken ct = default)
        {
            var snapshot = await _appointmentRepo.GetByIdAsync(proposalAppointmentId, ct);

            if (snapshot == null || !snapshot.RescheduledFromAppointmentId.HasValue)
            {
                return Result<AppointmentRescheduleResponseDto>.Fail(
                    AppointmentErrors.InvalidRescheduleProposal);
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var original = await _appointmentRepo.GetByIdForUpdateAsync(snapshot.RescheduledFromAppointmentId.Value, ct);

                var proposal = await _appointmentRepo.GetByIdForUpdateAsync(proposalAppointmentId, ct);

                if (original == null || proposal == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.InvalidRescheduleProposal);
                }

                var agreement =
                    await _agreementRepo.GetByIdAsync(original.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != userId && agreement.SellerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.Forbidden);
                }

                if (proposal.RescheduleRequestedByUserId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.CannotRespondOwnReschedule);
                }

                if (proposal.AppointmentStatus != (int)AppointmentStatus.Proposed || original.AppointmentStatus != (int)AppointmentStatus.Scheduled)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.InvalidRescheduleProposal);
                }

                if (original.AppointmentType == (int)AppointmentType.Inspection &&
                    (original.BuyerCheckAt.HasValue || original.SellerCheckAt.HasValue))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.CheckInAlreadyStarted);
                }

                var scheduleResult = await GetScheduleContextAsync(proposal, agreement, ct);

                if (!scheduleResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        scheduleResult.Error!);
                }

                var proposedAt = scheduleResult.Data!.ScheduledAt;

                var now = DateTime.UtcNow;

                if (now >= proposedAt)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(
                        AppointmentErrors.RescheduleProposalExpired);
                }

                original.AppointmentStatus = (int)AppointmentStatus.Cancelled;

                original.CancelledAt = now;
                original.CancellationReason = "Rescheduled";
                original.UpdatedAt = now;

                proposal.AppointmentStatus = (int)AppointmentStatus.Scheduled;

                proposal.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(original, ct);

                await _appointmentRepo.UpdateAsync(proposal, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<AppointmentRescheduleResponseDto>.Success(
                    new AppointmentRescheduleResponseDto
                    {
                        OriginalAppointmentId = original.AppointmentId,

                        ProposalAppointmentId = proposal.AppointmentId,

                        OriginalStatus = AppointmentStatus.Cancelled,

                        ProposalStatus = AppointmentStatus.Scheduled,

                        ProposedAt = proposedAt,

                        RequestedByUserId = proposal.RescheduleRequestedByUserId!.Value,

                        RequestedAt = proposal.RescheduleRequestedAt!.Value
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<AppointmentRescheduleResponseDto>> RejectRescheduleAsync(
            Guid proposalAppointmentId,
            Guid userId,
            RejectAppointmentRescheduleRequest request,
            CancellationToken ct = default)
        {
            var validation = await _rejectRescheduleValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
            {
                var message = string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage));

                return Result<AppointmentRescheduleResponseDto>.Fail(
                    new Error("Validation.InvalidRequest", message));
            }

            var snapshot = await _appointmentRepo.GetByIdAsync(proposalAppointmentId, ct);

            if (snapshot == null || !snapshot.RescheduledFromAppointmentId.HasValue)
                return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.InvalidRescheduleProposal);

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // Luôn lock original trước, proposal sau để thống nhất lock order với Accept.
                var original = await _appointmentRepo.GetByIdForUpdateAsync(snapshot.RescheduledFromAppointmentId.Value, ct);
                var proposal = await _appointmentRepo.GetByIdForUpdateAsync(proposalAppointmentId, ct);

                if (original == null || proposal == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.InvalidRescheduleProposal);
                }

                if (proposal.RescheduledFromAppointmentId != original.AppointmentId ||
                    proposal.AppointmentStatus != (int)AppointmentStatus.Proposed ||
                    original.AppointmentStatus != (int)AppointmentStatus.Scheduled)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.InvalidRescheduleProposal);
                }

                var agreement = await _agreementRepo.GetByIdAsync(original.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != userId && agreement.SellerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.Forbidden);
                }

                if (proposal.RescheduleRequestedByUserId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(AppointmentErrors.CannotRespondOwnReschedule);
                }

                var scheduleResult = await GetScheduleContextAsync(proposal, agreement, ct);

                if (!scheduleResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentRescheduleResponseDto>.Fail(scheduleResult.Error!);
                }

                var now = DateTime.UtcNow;

                proposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                proposal.CancelledAt = now;
                proposal.CancellationReason = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Reschedule request rejected."
                    : request.Reason.Trim();
                proposal.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(proposal, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<AppointmentRescheduleResponseDto>.Success(
                    new AppointmentRescheduleResponseDto
                    {
                        OriginalAppointmentId = original.AppointmentId,
                        ProposalAppointmentId = proposal.AppointmentId,
                        OriginalStatus = AppointmentStatus.Scheduled,
                        ProposalStatus = AppointmentStatus.Cancelled,
                        ProposedAt = scheduleResult.Data!.ScheduledAt,
                        RequestedByUserId = proposal.RescheduleRequestedByUserId!.Value,
                        RequestedAt = proposal.RescheduleRequestedAt!.Value
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<AppointmentActionResponseDto>> CancelAsync(
            Guid appointmentId,
            Guid userId,
            CancelAppointmentRequest request,
            CancellationToken ct = default)
        {
            var validation = await _cancelValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
            {
                var message = string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage));

                return Result<AppointmentActionResponseDto>.Fail(
                    new Error("Validation.InvalidRequest", message));
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var appointment = await _appointmentRepo.GetByIdForUpdateAsync(appointmentId, ct);

                if (appointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AppointmentErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != userId && agreement.SellerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AppointmentErrors.Forbidden);
                }

                if (appointment.AppointmentStatus != (int)AppointmentStatus.Scheduled ||
                    appointment.CancelledAt.HasValue ||
                    appointment.CompletedAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AppointmentErrors.InvalidStatus);
                }

                if (appointment.AppointmentType == (int)AppointmentType.Inspection &&
                    (appointment.BuyerCheckAt.HasValue || appointment.SellerCheckAt.HasValue))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AppointmentErrors.CheckInAlreadyStarted);
                }

                var scheduleResult = await GetScheduleContextAsync(appointment, agreement, ct);

                if (!scheduleResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(scheduleResult.Error!);
                }

                var schedule = scheduleResult.Data!;

                if (!SupportsUserAppointmentActions(schedule))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(AppointmentErrors.UnsupportedAction);
                }

                var policy = await _platformPolicyProvider.GetAppointmentConfigAsync(ct);
                var cutoff = schedule.ScheduledAt.AddHours(-policy.CancellationCutoffHours);
                var now = DateTime.UtcNow;

                if (now > cutoff)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<AppointmentActionResponseDto>.Fail(
                        AppointmentErrors.CancellationCutoffPassed(cutoff));
                }

                // Nếu đang có proposal đổi lịch thì cancel proposal cùng transaction.
                var pendingProposal = await _appointmentRepo.GetPendingRescheduleProposalAsync(
                    appointment.AppointmentId,
                    ct);

                if (pendingProposal != null)
                {
                    var lockedProposal = await _appointmentRepo.GetByIdForUpdateAsync(
                        pendingProposal.AppointmentId,
                        ct);

                    if (lockedProposal?.AppointmentStatus == (int)AppointmentStatus.Proposed)
                    {
                        lockedProposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                        lockedProposal.CancelledAt = now;
                        lockedProposal.CancellationReason = "Source appointment cancelled.";
                        lockedProposal.UpdatedAt = now;

                        await _appointmentRepo.UpdateAsync(lockedProposal, ct);
                    }
                }

                appointment.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                appointment.CancelledAt = now;
                appointment.CancellationReason = request.Reason.Trim();
                appointment.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(appointment, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<AppointmentActionResponseDto>.Success(
                    new AppointmentActionResponseDto
                    {
                        AppointmentId = appointment.AppointmentId,
                        AppointmentStatus = AppointmentStatus.Cancelled,
                        CancelledAt = appointment.CancelledAt,
                        CancellationReason = appointment.CancellationReason,
                        UpdatedAt = appointment.UpdatedAt
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        // =================== HELPER ======================

        private sealed class AppointmentScheduleContext
        {
            public AppointmentType AppointmentType { get; init; }
            public DateTime ScheduledAt { get; init; }

            public inspection_appointment? Inspection { get; init; }
            public collection_appointment? Collection { get; init; }

            public DeliveryMethod? DeliveryMethod { get; init; }
        }


        private async Task<Result<AppointmentScheduleContext>> GetScheduleContextAsync(
            appointment appointment,
            agreement_form agreement,
            CancellationToken ct)
        {
            if (appointment.AppointmentType == (int)AppointmentType.Inspection)
            {
                var inspection = await _inspectionRepo.GetByAppointmentIdAsync(appointment.AppointmentId, ct);

                if (inspection == null)
                    return Result<AppointmentScheduleContext>.Fail(AppointmentErrors.InspectionDetailNotFound);

                if (!inspection.InspectionDate.HasValue)
                    return Result<AppointmentScheduleContext>.Fail(AppointmentErrors.ScheduleMissing);

                return Result<AppointmentScheduleContext>.Success(
                    new AppointmentScheduleContext
                    {
                        AppointmentType = AppointmentType.Inspection,
                        ScheduledAt = inspection.InspectionDate.Value,
                        Inspection = inspection
                    });
            }

            if (appointment.AppointmentType == (int)AppointmentType.Collection)
            {
                var collection = await _collectionRepo.GetByAppointmentIdAsync(appointment.AppointmentId, ct);

                if (collection == null)
                    return Result<AppointmentScheduleContext>.Fail(AppointmentErrors.CollectionDetailNotFound);

                if (!collection.CollectionDate.HasValue)
                    return Result<AppointmentScheduleContext>.Fail(AppointmentErrors.ScheduleMissing);

                DeliveryMethod? deliveryMethod = null;

                if (Enum.TryParse<DeliveryMethod>(collection.DeliveryMethod, true, out var parsed))
                    deliveryMethod = parsed;

                if (!deliveryMethod.HasValue && !string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                {
                    try
                    {
                        var details = JsonSerializer.Deserialize<AgreementDetailsDto>(
                            agreement.AgreementDetailsJsonb,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        deliveryMethod = details?.DeliveryMethod;
                    }
                    catch (JsonException)
                    {
                        deliveryMethod = null;
                    }
                }

                return Result<AppointmentScheduleContext>.Success(
                    new AppointmentScheduleContext
                    {
                        AppointmentType = AppointmentType.Collection,
                        ScheduledAt = collection.CollectionDate.Value,
                        Collection = collection,
                        DeliveryMethod = deliveryMethod
                    });
            }

            return Result<AppointmentScheduleContext>.Fail(AppointmentErrors.InvalidType);
        }


        private static bool SupportsUserAppointmentActions(AppointmentScheduleContext context)
        {
            if (context.AppointmentType == AppointmentType.Inspection)
                return true;

            return context.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                   context.DeliveryMethod == DeliveryMethod.SellerDelivers;
        }

        private static bool SupportsLateThreshold(AppointmentScheduleContext context)
        {
            if (context.AppointmentType == AppointmentType.Inspection)
                return true;

            return context.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                   context.DeliveryMethod == DeliveryMethod.SellerDelivers;
        }
    }
}
