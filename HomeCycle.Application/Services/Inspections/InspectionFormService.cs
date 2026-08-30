using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Inspections;
using HomeCycle.Application.DTOs.Responses.Inspections;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Inspections;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Inspections;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Inspections
{
    public class InspectionFormService : IInspectionFormService
    {
        private const decimal AmountEpsilon = 0.01m;
        private const string InspectionMediaTargetType = "InspectionForm";
        private const string InspectionMediaFolder = "inspection-forms";

        private readonly IInspectionFormRepository _inspectionFormRepo;
        private readonly IInspectionAppointmentRepository _inspectionAppointmentRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IDisputeRepository _disputeRepo;
        private readonly IMediaService _mediaService;
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;

        private readonly IValidator<CreateInspectionFormRequest> _createValidator;
        private readonly IValidator<UpdateInspectionFormRequest> _updateValidator;
        private readonly IValidator<InspectionRevisionRequest> _revisionValidator;
        private readonly IValidator<RejectInspectionFormRequest> _rejectValidator;

        public InspectionFormService(IInspectionFormRepository inspectionFormRepo, IInspectionAppointmentRepository inspectionAppointmentRepo, IAppointmentRepository appointmentRepo, IAgreementFormRepository agreementRepo, IOrderRepository orderRepo, IDisputeRepository disputeRepo, IMediaService mediaService, IPaymentService paymentService, IUnitOfWork unitOfWork, IValidator<CreateInspectionFormRequest> createValidator, IValidator<UpdateInspectionFormRequest> updateValidator, IValidator<InspectionRevisionRequest> revisionValidator, IValidator<RejectInspectionFormRequest> rejectValidator)
        {
            _inspectionFormRepo = inspectionFormRepo;
            _inspectionAppointmentRepo = inspectionAppointmentRepo;
            _appointmentRepo = appointmentRepo;
            _agreementRepo = agreementRepo;
            _orderRepo = orderRepo;
            _disputeRepo = disputeRepo;
            _mediaService = mediaService;
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _revisionValidator = revisionValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<Result<InspectionFormResponseDto>> CreateDraftAsync(Guid appointmentId, Guid buyerId, CreateInspectionFormRequest request, CancellationToken ct = default)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
                return Result<InspectionFormResponseDto>.Fail(new Error("Validation.InvalidRequest", string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var appointment = await _appointmentRepo.GetByIdForUpdateAsync(appointmentId, ct);

                if (appointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(AppointmentErrors.NotFound);
                }

                if (appointment.AppointmentType != (int)AppointmentType.Inspection)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.InvalidAppointment);
                }

                var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BuyerOnly);
                }

                if (appointment.AppointmentStatus != (int)AppointmentStatus.InProgress)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.AppointmentNotInProgress);
                }

                if (!appointment.BuyerCheckAt.HasValue || !appointment.SellerCheckAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BothCheckInRequired);
                }

                var inspectionAppointment = await _inspectionAppointmentRepo.GetByAppointmentIdAsync(appointmentId, ct);

                if (inspectionAppointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(AppointmentErrors.InspectionDetailNotFound);
                }

                var existing = await _inspectionFormRepo.GetByInspectionAppointmentIdAsync(inspectionAppointment.InspectionAppointmentId, ct);

                if (existing != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.AlreadyExists);
                }

                var order = await _orderRepo.GetByAgreementIdAsync(agreement.AgreementId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(OrderErrors.NotFound);
                }

                var originalPrice = order.FinalTotalAmount ?? order.OriginalTotalAmount;

                if (!originalPrice.HasValue || originalPrice.Value <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.InvalidOrderPrice);
                }

                var now = DateTime.UtcNow;

                var form = new inspection_form
                {
                    InspectionFormId = Guid.NewGuid(),
                    InspectionAppointmentId = inspectionAppointment.InspectionAppointmentId,
                    OrderId = order.OrderId,
                    InspectorId = buyerId,

                    InspectionTime = now,

                    OperatingStatus = request.OperatingStatus?.ToString(),
                    AppearanceStatus = request.AppearanceStatus?.ToString(),
                    PartsStatus = request.PartsStatus?.ToString(),
                    MatchStatus = request.MatchStatus?.ToString(),
                    InspectorNotes = string.IsNullOrWhiteSpace(request.InspectorNotes) ? null : request.InspectorNotes.Trim(),

                    Conclusion = request.Conclusion?.ToString(),
                    OriginalPrice = originalPrice.Value,

                    SuggestedPrice = request.Conclusion == InspectionConclusion.PriceAdjustment
                        ? request.SuggestedPrice
                        : null,

                    InspectionStatus = (int)InspectionStatus.Draft,
                    Revision = 1,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _inspectionFormRepo.AddAsync(form, ct);

                if (request.Images?.Any(x => x.Length > 0) == true)
                {
                    var mediaResult = await _mediaService.UploadAndSaveMediaAsync(form.InspectionFormId, InspectionMediaTargetType, InspectionMediaFolder, request.Images, ct);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<InspectionFormResponseDto>.Fail(mediaResult.Error!);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, buyerId, ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<InspectionFormResponseDto>> UpdateDraftAsync(Guid inspectionFormId, Guid buyerId, UpdateInspectionFormRequest request, CancellationToken ct = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
                return Result<InspectionFormResponseDto>.Fail(new Error("Validation.InvalidRequest", string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(inspectionFormId, ct);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.NotFound);
                }

                if (form.InspectorId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BuyerOnly);
                }

                if (form.InspectionStatus != (int)InspectionStatus.Draft)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.DraftOnly);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.RevisionMismatch);
                }

                form.OperatingStatus = request.OperatingStatus?.ToString();
                form.AppearanceStatus = request.AppearanceStatus?.ToString();
                form.PartsStatus = request.PartsStatus?.ToString();
                form.MatchStatus = request.MatchStatus?.ToString();
                form.InspectorNotes = string.IsNullOrWhiteSpace(request.InspectorNotes) ? null : request.InspectorNotes.Trim();
                form.Conclusion = request.Conclusion?.ToString();
                form.SuggestedPrice = request.Conclusion == InspectionConclusion.PriceAdjustment ? request.SuggestedPrice : null;

                form.Revision++;
                form.UpdatedAt = DateTime.UtcNow;

                await _inspectionFormRepo.UpdateAsync(form, ct);

                if (request.ReplaceImages)
                {
                    Result<bool>? deleteResult = null;

                    if (request.Images?.Any(x => x.Length > 0) == true)
                    {
                        var replaceResult = await _mediaService.ReplaceMediaAsync(form.InspectionFormId, InspectionMediaTargetType, InspectionMediaFolder, request.Images, ct);

                        if (!replaceResult.IsSuccess)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return Result<InspectionFormResponseDto>.Fail(replaceResult.Error!);
                        }
                    }
                    else
                    {
                        deleteResult = await _mediaService.DeleteByTargetAsync(form.InspectionFormId, InspectionMediaTargetType, ct);

                        if (!deleteResult.IsSuccess)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return Result<InspectionFormResponseDto>.Fail(deleteResult.Error!);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, buyerId, ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<InspectionFormResponseDto>> SubmitAsync(Guid inspectionFormId, Guid buyerId, InspectionRevisionRequest request, CancellationToken ct = default)
        {
            var validation = await _revisionValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
                return Result<InspectionFormResponseDto>.Fail(new Error("Validation.InvalidRequest", string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(inspectionFormId, ct);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.NotFound);
                }

                if (form.InspectorId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BuyerOnly);
                }

                if (form.InspectionStatus != (int)InspectionStatus.Draft)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.DraftOnly);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.RevisionMismatch);
                }

                if (!IsComplete(form))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.Incomplete);
                }

                var conclusion = ParseEnum<InspectionConclusion>(form.Conclusion);

                if (conclusion == InspectionConclusion.PriceAdjustment)
                {
                    if (!form.SuggestedPrice.HasValue || form.SuggestedPrice.Value <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<InspectionFormResponseDto>.Fail(InspectionErrors.SuggestedPriceRequired);
                    }

                    if (form.SuggestedPrice == form.OriginalPrice)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<InspectionFormResponseDto>.Fail(InspectionErrors.SuggestedPriceUnchanged);
                    }
                }

                var inspection = await _inspectionAppointmentRepo.GetByIdAsync(form.InspectionAppointmentId, ct);

                if (inspection == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.InvalidAppointment);
                }

                var appointment = await _appointmentRepo.GetByIdForUpdateAsync(inspection.AppointmentId, ct);

                if (appointment == null || appointment.AppointmentStatus != (int)AppointmentStatus.InProgress)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.AppointmentNotInProgress);
                }

                if (!appointment.BuyerCheckAt.HasValue || !appointment.SellerCheckAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BothCheckInRequired);
                }

                var now = DateTime.UtcNow;

                form.InspectionStatus = (int)InspectionStatus.PendingSellerConfirmation;
                form.SubmittedAt = now;
                form.UpdatedAt = now;

                await _inspectionFormRepo.UpdateAsync(form, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, buyerId, ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<InspectionFormResponseDto>> SellerRejectAsync(Guid inspectionFormId, Guid sellerId, RejectInspectionFormRequest request, CancellationToken ct = default)
        {
            var validation = await _rejectValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
                return Result<InspectionFormResponseDto>.Fail(new Error("Validation.InvalidRequest", string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(inspectionFormId, ct);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.NotFound);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.RevisionMismatch);
                }

                if (form.InspectionStatus != (int)InspectionStatus.PendingSellerConfirmation)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.PendingConfirmationOnly);
                }

                var order = await _orderRepo.GetByIdAsync(form.OrderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.SellerOnly);
                }

                var now = DateTime.UtcNow;

                form.InspectionStatus = (int)InspectionStatus.Rejected;
                form.SellerDecisionAt = now;
                form.SellerDecisionReason = request.Reason.Trim();
                form.UpdatedAt = now;

                await _inspectionFormRepo.UpdateAsync(form, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, sellerId, ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<InspectionFormResponseDto>> SellerConfirmAsync(
            Guid inspectionFormId,
            Guid sellerId,
            InspectionRevisionRequest request,
            CancellationToken ct = default)
        {
            var validation = await _revisionValidator.ValidateAsync(request, ct);

            if (!validation.IsValid)
            {
                return Result<InspectionFormResponseDto>.Fail(
                    new Error(
                        "Validation.InvalidRequest",
                        string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(
                    inspectionFormId,
                    ct);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.NotFound);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.RevisionMismatch);
                }

                if (form.InspectionStatus !=
                    (int)InspectionStatus.PendingSellerConfirmation)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.PendingConfirmationOnly);
                }

                var order = await _orderRepo.GetByIdForUpdateAsync(
                    form.OrderId,
                    ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        OrderErrors.NotFound);
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        OrderErrors.InvalidStatus);
                }

                var agreement = await _agreementRepo.GetByIdAsync(
                    order.AgreementId,
                    ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        AgreementErrors.NotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.SellerOnly);
                }

                var inspection =
                    await _inspectionAppointmentRepo.GetByIdAsync(
                        form.InspectionAppointmentId,
                        ct);

                if (inspection == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.InvalidAppointment);
                }

                var appointment =
                    await _appointmentRepo.GetByIdForUpdateAsync(
                        inspection.AppointmentId,
                        ct);

                if (appointment == null ||
                    appointment.AppointmentStatus !=
                        (int)AppointmentStatus.InProgress)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.AppointmentNotInProgress);
                }

                var conclusion =
                    ParseEnum<InspectionConclusion>(
                        form.Conclusion);

                if (!conclusion.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<InspectionFormResponseDto>.Fail(
                        InspectionErrors.Incomplete);
                }

                var now = DateTime.UtcNow;

                form.InspectionStatus =
                    (int)InspectionStatus.Accepted;

                form.SellerDecisionAt = now;
                form.SellerDecisionReason = null;
                form.UpdatedAt = now;

                appointment.AppointmentStatus =
                    (int)AppointmentStatus.Completed;

                appointment.CompletedAt = now;
                appointment.UpdatedAt = now;


                // PRICE ADJUSTMENT
                if (conclusion == InspectionConclusion.PriceAdjustment)
                {
                    if (!form.SuggestedPrice.HasValue ||
                        form.SuggestedPrice.Value <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<InspectionFormResponseDto>.Fail(
                            InspectionErrors.SuggestedPriceRequired);
                    }

                    var newTotal = form.SuggestedPrice.Value;
                    var currentAmountPaid = order.AmountPaid ?? 0;

                   
                    if (currentAmountPaid > newTotal + AmountEpsilon)
                    {
                        var refundAmount =
                            currentAmountPaid - newTotal;

                        var refundResult =
                            await _paymentService
                                .RefundOrderHeldAmountAsync(
                                    order,
                                    agreement,
                                    refundAmount,
                                    ct);

                        if (!refundResult.IsSuccess)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<InspectionFormResponseDto>.Fail(
                                refundResult.Error!);
                        }

                        // AmountPaid phải phản ánh số tiền NET
                        // còn được áp dụng cho Order sau refund.
                        order.AmountPaid =
                            currentAmountPaid - refundAmount;
                    }

                    order.FinalTotalAmount = newTotal;

                    var effectiveAmountPaid =
                        order.AmountPaid ?? 0;

                    order.AmountRemaining =
                        Math.Max(
                            newTotal - effectiveAmountPaid,
                            0);


                    // còn tiền trả trực tiếp -> Pending
                    // đã đủ theo final price -> Completed.
                    order.PaymentStatus =
                        order.AmountRemaining <= AmountEpsilon
                            ? (int)PaymentStatus.Completed
                            : (int)PaymentStatus.Pending;

                    order.UpdatedAt = now;
                }

                // FAILED INSPECTION
                else if (conclusion == InspectionConclusion.Failed)
                {
                    var deposit =
                        order.AmountPaid ?? 0;

                    if (deposit <= AmountEpsilon)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<InspectionFormResponseDto>.Fail(
                            InspectionErrors.DepositMissing);
                    }

                    var refundResult =
                        await _paymentService
                            .RefundOrderHeldAmountAsync(
                                order,
                                agreement,
                                deposit,
                                ct);

                    if (!refundResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<InspectionFormResponseDto>.Fail(
                            refundResult.Error!);
                    }

                    // Refund full.
                    order.AmountPaid = 0;
                    order.AmountRemaining = 0;

                    order.PaymentStatus =
                        (int)PaymentStatus.Refunded;

                    order.OrderStatus =
                        (int)OrderStatus.Cancelled;

                    order.CancelledAt = now;
                    order.CancelledByUserId = sellerId;

                    order.CancellationReason =
                        "Inspection result confirmed as failed.";

                    order.DisputeWindowEndsAt = null;
                    order.UpdatedAt = now;
                }

                await _inspectionFormRepo.UpdateAsync(form, ct);
                await _appointmentRepo.UpdateAsync(appointment, ct);
                await _orderRepo.UpdateAsync(order, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(
                    await BuildResponseAsync(
                        form,
                        sellerId,
                        ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<InspectionFormResponseDto>> CollectNowAsync(Guid inspectionFormId, Guid buyerId, InspectionRevisionRequest request, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(inspectionFormId, ct);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.NotFound);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.RevisionMismatch);
                }

                if (form.InspectionStatus != (int)InspectionStatus.Accepted)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.AcceptedRequired);
                }

                if (ParseEnum<InspectionConclusion>(form.Conclusion) == InspectionConclusion.Failed)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.FailedCannotCollect);
                }

                if (!string.IsNullOrWhiteSpace(form.CollectAction))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.CollectActionAlreadySelected);
                }

                var order = await _orderRepo.GetByIdAsync(form.OrderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(InspectionErrors.BuyerOnly);
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<InspectionFormResponseDto>.Fail(OrderErrors.InvalidStatus);
                }

                form.CollectAction = InspectionCollectAction.CollectNow.ToString();
                form.UpdatedAt = DateTime.UtcNow;

                await _inspectionFormRepo.UpdateAsync(form, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, buyerId, ct));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<InspectionFormResponseDto>> GetByAppointmentAsync(Guid appointmentId, Guid userId, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(appointmentId, ct);

            if (appointment == null)
                return Result<InspectionFormResponseDto>.Fail(AppointmentErrors.NotFound);

            if (appointment.AppointmentType != (int)AppointmentType.Inspection)
                return Result<InspectionFormResponseDto>.Fail(InspectionErrors.InvalidAppointment);

            var agreement = await _agreementRepo.GetByIdAsync(appointment.AgreementId, ct);

            if (agreement == null)
                return Result<InspectionFormResponseDto>.Fail(AgreementErrors.NotFound);

            if (agreement.BuyerId != userId && agreement.SellerId != userId)
                return Result<InspectionFormResponseDto>.Fail(AppointmentErrors.Forbidden);

            var inspection = await _inspectionAppointmentRepo.GetByAppointmentIdAsync(appointmentId, ct);

            if (inspection == null)
                return Result<InspectionFormResponseDto>.Fail(AppointmentErrors.InspectionDetailNotFound);

            var form = await _inspectionFormRepo.GetByInspectionAppointmentIdAsync(inspection.InspectionAppointmentId, ct);

            if (form == null)
                return Result<InspectionFormResponseDto>.Fail(InspectionErrors.NotFound);

            return Result<InspectionFormResponseDto>.Success(await BuildResponseAsync(form, userId, ct));
        }


        #region HELPER

        private async Task<InspectionFormResponseDto> BuildResponseAsync(inspection_form form, Guid currentUserId, CancellationToken ct)
        {
            var inspection = await _inspectionAppointmentRepo.GetByIdAsync(form.InspectionAppointmentId, ct);
            var order = await _orderRepo.GetByIdAsync(form.OrderId, ct);

            if (inspection == null || order == null)
                throw new InvalidOperationException("Inspection form references invalid data.");

            var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

            if (agreement == null)
                throw new InvalidOperationException("Agreement not found.");

            var isBuyer = agreement.BuyerId == currentUserId;
            var isSeller = agreement.SellerId == currentUserId;

            var mediaResult = await _mediaService.GetByTargetsAsync(new[] { form.InspectionFormId }, InspectionMediaTargetType, ct);

            IReadOnlyList<MediaResponse> images = Array.Empty<MediaResponse>();

            if (mediaResult.IsSuccess &&
                mediaResult.Data != null &&
                mediaResult.Data.TryGetValue(form.InspectionFormId, out var foundImages))
            {
                images = foundImages;
            }

            var hasActiveDispute = await _disputeRepo.ExistsActiveAsync(DisputeTargetType.Order, order.OrderId, ct);

            var status = (InspectionStatus)form.InspectionStatus;
            var conclusion = ParseEnum<InspectionConclusion>(form.Conclusion);
            var collectAction = ParseEnum<InspectionCollectAction>(form.CollectAction);

            return new InspectionFormResponseDto
            {
                InspectionFormId = form.InspectionFormId,
                AppointmentId = inspection.AppointmentId,
                InspectionAppointmentId = form.InspectionAppointmentId,
                OrderId = form.OrderId,
                InspectorId = form.InspectorId,

                Revision = form.Revision,
                InspectionStatus = status,

                InspectionTime = form.InspectionTime,

                OperatingStatus = ParseEnum<InspectionOperatingStatus>(form.OperatingStatus),
                AppearanceStatus = ParseEnum<InspectionAppearanceStatus>(form.AppearanceStatus),
                PartsStatus = ParseEnum<InspectionPartsStatus>(form.PartsStatus),
                MatchStatus = ParseEnum<InspectionMatchStatus>(form.MatchStatus),

                InspectorNotes = form.InspectorNotes,
                Conclusion = conclusion,

                OriginalPrice = form.OriginalPrice,
                SuggestedPrice = form.SuggestedPrice,
                CollectAction = collectAction,

                SubmittedAt = form.SubmittedAt,
                SellerDecisionAt = form.SellerDecisionAt,
                SellerDecisionReason = form.SellerDecisionReason,

                CreatedAt = form.CreatedAt,
                UpdatedAt = form.UpdatedAt,

                Images = images,

                Order = new InspectionOrderSummaryDto
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    OrderStatus = order.OrderStatus.HasValue ? (OrderStatus?)order.OrderStatus.Value : null,
                    PaymentStatus = order.PaymentStatus.HasValue ? (PaymentStatus?)order.PaymentStatus.Value : null,
                    OriginalTotalAmount = order.OriginalTotalAmount,
                    FinalTotalAmount = order.FinalTotalAmount,
                    AmountPaid = order.AmountPaid,
                    AmountRemaining = order.AmountRemaining
                },

                Actions = new InspectionFormActionDto
                {
                    CanEdit = isBuyer && status == InspectionStatus.Draft,
                    CanSubmit = isBuyer && status == InspectionStatus.Draft,

                    CanSellerConfirm = isSeller && status == InspectionStatus.PendingSellerConfirmation,
                    CanSellerReject = isSeller && status == InspectionStatus.PendingSellerConfirmation,

                    CanCollectNow =
                        isBuyer &&
                        status == InspectionStatus.Accepted &&
                        conclusion != InspectionConclusion.Failed &&
                        !collectAction.HasValue &&
                        order.OrderStatus == (int)OrderStatus.Processing,

                    CanCancelTransaction =
                        (isBuyer || isSeller) &&
                        status == InspectionStatus.Rejected &&
                        order.OrderStatus == (int)OrderStatus.Processing &&
                        !hasActiveDispute
                }
            };
        }


        private static bool IsComplete(inspection_form form)
        {
            return !string.IsNullOrWhiteSpace(form.OperatingStatus) &&
                   !string.IsNullOrWhiteSpace(form.AppearanceStatus) &&
                   !string.IsNullOrWhiteSpace(form.PartsStatus) &&
                   !string.IsNullOrWhiteSpace(form.MatchStatus) &&
                   !string.IsNullOrWhiteSpace(form.Conclusion);
        }


        private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct, Enum
        {
            return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : null;
        }

        #endregion


    }
}
