using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Inspections;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IReviewRepository _reviewRepo;
        private readonly IDisputeWindowPolicy _disputeWindowPolicy;
        private readonly ICollectionAppointmentRepository _collectionAppointmentRepo;
        private readonly IInspectionFormRepository _inspectionFormRepo;
        private readonly IInspectionAppointmentRepository _inspectionAppointmentRepo;
        private readonly IDisputeRepository _disputeRepo;
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;

        public OrderService(
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IAppointmentRepository appointmentRepo,
            IShipmentRepository shipmentRepo,
            //IReviewRepository reviewRepo,
            IDisputeWindowPolicy disputeWindowPolicy,
            ICollectionAppointmentRepository collectionAppointmentRepo,
            IUnitOfWork unitOfWork,
            IInspectionFormRepository inspectionFormRepo,
            IInspectionAppointmentRepository inspectionAppointmentRepo,
            IDisputeRepository disputeRepo,
            IPaymentService paymentService,
            IMapper mapper)
        {
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _appointmentRepo = appointmentRepo;
            _shipmentRepo = shipmentRepo;
            //_reviewRepo = reviewRepo;
            _disputeWindowPolicy = disputeWindowPolicy;
            _collectionAppointmentRepo = collectionAppointmentRepo;
            _unitOfWork = unitOfWork;
            _inspectionFormRepo = inspectionFormRepo;
            _inspectionAppointmentRepo = inspectionAppointmentRepo;
            _disputeRepo = disputeRepo;
            _paymentService = paymentService;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<OrderListItemDto>>> GetMyOrdersAsync(
            Guid userId, bool isSeller, OrderSearchRequest request, CancellationToken ct = default)
        {
            var result = await _orderRepo.GetPagedByUserAsync(userId, isSeller, request, ct);
            return Result<PagedResult<OrderListItemDto>>.Success(result);
        }

        public async Task<Result<OrderDetailDto>> GetDetailAsync(Guid orderId, Guid userId, CancellationToken ct = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, ct);

            if (order == null)
                return Result<OrderDetailDto>.Fail(OrderErrors.NotFound);

            var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

            if (agreement == null)
                return Result<OrderDetailDto>.Fail(AgreementErrors.NotFound);

            var isBuyer = agreement.BuyerId == userId;
            var isSeller = agreement.SellerId == userId;

            if (!isBuyer && !isSeller)
                return Result<OrderDetailDto>.Fail(OrderErrors.Forbidden);

            var detail = await _orderRepo.GetDetailWithRelationsAsync(orderId, userId, ct);

            if (detail == null)
                return Result<OrderDetailDto>.Fail(OrderErrors.NotFound);

            detail.Appointments =
                await _appointmentRepo.GetAppointmentSummariesByAgreementIdAsync(
                    order.AgreementId,
                    ct);

            var myReview = detail.Reviews
                .FirstOrDefault(r => r.ReviewerId == userId);

            detail.Review = new ReviewSummaryDto
            {
                ReviewId = myReview?.ReviewId,
                HasReviewed = myReview != null,
                Rating = myReview?.Rating
            };

            detail.Actions = await BuildOrderActionsAsync(
                detail,
                agreement,
                isBuyer,
                isSeller,
                ct);

            return Result<OrderDetailDto>.Success(detail);
        }

        public async Task<Result<OrderReferenceDto>> GetByAgreementAsync(Guid agreementId, Guid userId, CancellationToken ct = default)
        {
            var authResult = await CheckOwnershipAsync(agreementId, userId, ct);

            if (!authResult.IsSuccess)
                return Result<OrderReferenceDto>.Fail(authResult.Error!);

            var order = await _orderRepo.GetByAgreementIdAsync(agreementId, ct);

            if (order == null)
                return Result<OrderReferenceDto>.Fail(OrderErrors.NotCreated);

            return Result<OrderReferenceDto>.Success(
                _mapper.Map<OrderReferenceDto>(order));
        }

        public async Task<Result<OrderConfirmationResponseDto>> ConfirmHandoverAsync(Guid orderId, Guid sellerId, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                if (order.OrderStatus == (int)OrderStatus.Completed)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderConfirmationResponseDto>.Success(
                        new OrderConfirmationResponseDto
                        {
                            OrderId = order.OrderId,
                            OrderStatus = order.OrderStatus,
                            SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                            BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                            CompletedAt = order.CompletedAt,

                            CompletionSource = order.CompletionSource.HasValue
                                ? (OrderCompletionSource?)order.CompletionSource.Value
                                : null
                        });
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.InvalidStatus);
                }

                var inspectionCollectNow =
                    await IsInspectionCollectNowReadyAsync(
                        order.OrderId,
                        ct);

                appointment? lockedCollection = null;

                if (!inspectionCollectNow)
                {
                    var deliveryMethodResult =
                        ResolveDeliveryMethod(agreement);

                    if (!deliveryMethodResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            deliveryMethodResult.Error!);
                    }

                    var deliveryMethod = deliveryMethodResult.Data;

                    if (
                        deliveryMethod != DeliveryMethod.BuyerPickUp &&
                        deliveryMethod != DeliveryMethod.SellerDelivers)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            OrderErrors.DirectHandoverOnly);
                    }

                    var collectionAppointment =
                        await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                            agreement.AgreementId,
                            AppointmentType.Collection,
                            ct);

                    if (collectionAppointment == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.NotFound);
                    }

                    var collection =
                        await _collectionAppointmentRepo.GetByAppointmentIdAsync(
                            collectionAppointment.AppointmentId,
                            ct);

                    if (collection == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.CollectionDetailNotFound);
                    }

                    if (!collection.CollectionDate.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.ScheduleMissing);
                    }

                    var now = DateTime.UtcNow;

                    if (!IsCollectionConfirmationOpen(
                        collection.CollectionDate.Value,
                        now))
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.CollectionConfirmationNotOpen(
                                collection.CollectionDate.Value));
                    }

                    lockedCollection =
                        await _appointmentRepo.GetByIdForUpdateAsync(
                            collectionAppointment.AppointmentId,
                            ct);

                    if (lockedCollection == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.NotFound);
                    }

                    if (
                        lockedCollection.AppointmentStatus !=
                            (int)AppointmentStatus.Scheduled &&
                        lockedCollection.AppointmentStatus !=
                            (int)AppointmentStatus.InProgress)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            AppointmentErrors.InvalidStatus);
                    }
                }

                var confirmedAt = DateTime.UtcNow;
                var changed = false;

                if (!order.SellerHandoverConfirmedAt.HasValue)
                {
                    order.SellerHandoverConfirmedAt = confirmedAt;
                    order.UpdatedAt = confirmedAt;

                    await _orderRepo.UpdateAsync(order, ct);

                    changed = true;
                }

                if (
                    lockedCollection != null &&
                    lockedCollection.AppointmentStatus ==
                        (int)AppointmentStatus.Scheduled)
                {
                    lockedCollection.AppointmentStatus =
                        (int)AppointmentStatus.InProgress;

                    lockedCollection.UpdatedAt = confirmedAt;

                    await _appointmentRepo.UpdateAsync(
                        lockedCollection,
                        ct);

                    changed = true;
                }

                if (changed)
                    await _unitOfWork.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<OrderConfirmationResponseDto>.Success(
                    new OrderConfirmationResponseDto
                    {
                        OrderId = order.OrderId,
                        OrderStatus = order.OrderStatus,
                        SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                        BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                        CompletedAt = order.CompletedAt,

                        CompletionSource = order.CompletionSource.HasValue
                            ? (OrderCompletionSource?)order.CompletionSource.Value
                            : null
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<OrderConfirmationResponseDto>> ConfirmReceivedAsync(Guid orderId, Guid buyerId, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(
                        OrderErrors.NotFound);
                }

                var agreement =
                    await _agreementRepo.GetByIdAsync(
                        order.AgreementId,
                        ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(
                        AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(
                        OrderErrors.Forbidden);
                }

                if (order.OrderStatus == (int)OrderStatus.Completed)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderConfirmationResponseDto>.Success(
                        new OrderConfirmationResponseDto
                        {
                            OrderId = order.OrderId,
                            OrderStatus = order.OrderStatus,
                            SellerHandoverConfirmedAt =
                                order.SellerHandoverConfirmedAt,
                            BuyerReceivedConfirmedAt =
                                order.BuyerReceivedConfirmedAt,
                            CompletedAt = order.CompletedAt,

                            CompletionSource =
                                order.CompletionSource.HasValue
                                    ? (OrderCompletionSource?)order.CompletionSource.Value
                                    : null
                        });
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(
                        OrderErrors.InvalidStatus);
                }

                var inspectionCollectNow =
                    await IsInspectionCollectNowReadyAsync(
                        order.OrderId,
                        ct);

                appointment? directCollection = null;

                if (!inspectionCollectNow)
                {
                    var deliveryMethodResult =
                        ResolveDeliveryMethod(agreement);

                    if (!deliveryMethodResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<OrderConfirmationResponseDto>.Fail(
                            deliveryMethodResult.Error!);
                    }

                    var deliveryMethod = deliveryMethodResult.Data;

                    if (deliveryMethod == DeliveryMethod.GhnDelivery)
                    {
                        var shipment =
                            await _shipmentRepo.GetByOrderIdAsync(
                                order.OrderId,
                                ct);

                        if (shipment == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return Result<OrderConfirmationResponseDto>.Fail(
                                OrderErrors.ShipmentNotFound);
                        }

                        if (
                            shipment.ShipmentStatus !=
                                ShipmentStatus.Delivered ||
                            !shipment.DeliveredAt.HasValue)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                OrderErrors.ShipmentNotDelivered);
                        }
                    }
                    else if (
                        deliveryMethod == DeliveryMethod.BuyerPickUp ||
                        deliveryMethod == DeliveryMethod.SellerDelivers)
                    {
                        var collectionAppointment =
                            await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                                agreement.AgreementId,
                                AppointmentType.Collection,
                                ct);

                        if (collectionAppointment == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.NotFound);
                        }

                        var collection =
                            await _collectionAppointmentRepo.GetByAppointmentIdAsync(
                                collectionAppointment.AppointmentId,
                                ct);

                        if (collection == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.CollectionDetailNotFound);
                        }

                        if (!collection.CollectionDate.HasValue)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.ScheduleMissing);
                        }

                        var now = DateTime.UtcNow;

                        if (!IsCollectionConfirmationOpen(
                            collection.CollectionDate.Value,
                            now))
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.CollectionConfirmationNotOpen(
                                    collection.CollectionDate.Value));
                        }

                        directCollection =
                            await _appointmentRepo.GetByIdForUpdateAsync(
                                collectionAppointment.AppointmentId,
                                ct);

                        if (directCollection == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.NotFound);
                        }

                        if (
                            directCollection.AppointmentStatus !=
                                (int)AppointmentStatus.Scheduled &&
                            directCollection.AppointmentStatus !=
                                (int)AppointmentStatus.InProgress)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);

                            return Result<OrderConfirmationResponseDto>.Fail(
                                AppointmentErrors.InvalidStatus);
                        }
                    }
                    else
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<OrderConfirmationResponseDto>.Fail(
                            OrderErrors.DeliveryMethodMissing);
                    }
                }

                var completedAt = DateTime.UtcNow;

                var disputeWindow =
                    await _disputeWindowPolicy.GetOrderDisputeWindowAsync(
                        agreement.SellerId,
                        ct);

                order.BuyerReceivedConfirmedAt = completedAt;
                order.OrderStatus = (int)OrderStatus.Completed;
                // nếu Order trước đó là Deposit -> Pending,
                // Buyer confirm nhận hàng nghĩa là giao dịch trực tiếp
                // đã hoàn tất nên Order payment chuyển Completed.
                order.PaymentStatus =
                    (int)PaymentStatus.Completed;
                order.CompletedAt = completedAt;
                order.CompletionSource =
                    (int)OrderCompletionSource.BuyerConfirmed;

                order.DisputeWindowEndsAt ??=
                    completedAt.Add(disputeWindow);

                order.UpdatedAt = completedAt;

                await _orderRepo.UpdateAsync(order, ct);

                if (directCollection != null)
                {
                    directCollection.AppointmentStatus =
                        (int)AppointmentStatus.Completed;

                    directCollection.CompletedAt = completedAt;
                    directCollection.UpdatedAt = completedAt;

                    await _appointmentRepo.UpdateAsync(
                        directCollection,
                        ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<OrderConfirmationResponseDto>.Success(
                    new OrderConfirmationResponseDto
                    {
                        OrderId = order.OrderId,
                        OrderStatus = order.OrderStatus,
                        SellerHandoverConfirmedAt =
                            order.SellerHandoverConfirmedAt,
                        BuyerReceivedConfirmedAt =
                            order.BuyerReceivedConfirmedAt,
                        CompletedAt = order.CompletedAt,

                        CompletionSource =
                            order.CompletionSource.HasValue
                                ? (OrderCompletionSource?)order.CompletionSource.Value
                                : null
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<OrderCancellationResponseDto>> CancelAfterRejectedInspectionAsync(
            Guid orderId,
            Guid userId,
            CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order =
                    await _orderRepo.GetByIdForUpdateAsync(
                        orderId,
                        ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        OrderErrors.NotFound);
                }

                var agreement =
                    await _agreementRepo.GetByIdAsync(
                        order.AgreementId,
                        ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        AgreementErrors.NotFound);
                }

                var isBuyer =
                    agreement.BuyerId == userId;

                var isSeller =
                    agreement.SellerId == userId;

                // Cả Buyer và Seller đều được cancel
                // sau rejected inspection.
                if (!isBuyer && !isSeller)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        OrderErrors.Forbidden);
                }

                // Idempotent cho retry sau request thành công.
                if (order.OrderStatus == (int)OrderStatus.Cancelled &&
                    order.CancelledAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Success(
                        new OrderCancellationResponseDto
                        {
                            OrderId = order.OrderId,

                            OrderStatus =
                                OrderStatus.Cancelled,

                            PaymentStatus =
                                order.PaymentStatus.HasValue
                                    ? (PaymentStatus?)
                                        order.PaymentStatus.Value
                                    : null,

                            CancelledAt =
                                order.CancelledAt.Value,

                            CancelledByUserId =
                                order.CancelledByUserId,

                            CancellationReason =
                                order.CancellationReason
                        });
                }

                if (order.OrderStatus !=
                    (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        OrderErrors.InvalidStatus);
                }

                var inspectionForm =
                    await _inspectionFormRepo.GetLatestByOrderIdAsync(
                        order.OrderId,
                        ct);

                if (inspectionForm == null ||
                    inspectionForm.InspectionStatus !=
                        (int)InspectionStatus.Rejected)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        OrderErrors.CancellationRequiresRejectedInspection);
                }

                var hasActiveDispute =
                    await _disputeRepo.ExistsActiveAsync(
                        DisputeTargetType.Order,
                        order.OrderId,
                        ct);

                if (hasActiveDispute)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        OrderErrors.ActiveDisputeBlocksCancellation);
                }

                var inspectionAppointment =
                    await _inspectionAppointmentRepo.GetByIdAsync(
                        inspectionForm.InspectionAppointmentId,
                        ct);

                if (inspectionAppointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        InspectionErrors.InvalidAppointment);
                }

                var appointment =
                    await _appointmentRepo.GetByIdForUpdateAsync(
                        inspectionAppointment.AppointmentId,
                        ct);

                if (appointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        AppointmentErrors.NotFound);
                }

                var amountToRefund =
                    order.AmountPaid ?? 0;

                if (amountToRefund <= 0.01m)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        PaymentErrors.InvalidRefundAmount);
                }

                var refundResult =
                    await _paymentService
                        .RefundOrderHeldAmountAsync(
                            order,
                            agreement,
                            amountToRefund,
                            ct);

                if (!refundResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Fail(
                        refundResult.Error!);
                }

                var now = DateTime.UtcNow;

                // Appointment đã thật sự diễn ra:
                // check-in + inspection + form + seller reject.
                // Vì vậy Completed hợp lý hơn Cancelled.
                appointment.AppointmentStatus =
                    (int)AppointmentStatus.Completed;

                appointment.CompletedAt ??= now;
                appointment.UpdatedAt = now;

                order.OrderStatus =
                    (int)OrderStatus.Cancelled;

                order.PaymentStatus =
                    (int)PaymentStatus.Refunded;

                order.AmountPaid = 0;
                order.AmountRemaining = 0;

                order.CancelledAt = now;
                order.CancelledByUserId = userId;

                order.CancellationReason =
                    !string.IsNullOrWhiteSpace(
                        inspectionForm.SellerDecisionReason)
                        ? inspectionForm.SellerDecisionReason
                        : "Transaction cancelled after rejected inspection result.";

                order.DisputeWindowEndsAt = null;
                order.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(
                    appointment,
                    ct);

                await _orderRepo.UpdateAsync(
                    order,
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<OrderCancellationResponseDto>.Success(
                    new OrderCancellationResponseDto
                    {
                        OrderId = order.OrderId,

                        OrderStatus =
                            OrderStatus.Cancelled,

                        PaymentStatus =
                            PaymentStatus.Refunded,

                        CancelledAt = now,
                        CancelledByUserId = userId,

                        CancellationReason =
                            order.CancellationReason
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        //================ HELPER =======================

        private async Task<bool> IsInspectionCollectNowReadyAsync(
            Guid orderId,
            CancellationToken ct)
        {
            var form =
                await _inspectionFormRepo
                    .GetAcceptedCollectNowByOrderIdAsync(
                        orderId,
                        ct);

            if (form == null)
                return false;

            if (!Enum.TryParse<InspectionConclusion>(
                form.Conclusion,
                true,
                out var conclusion))
            {
                return false;
            }

            return conclusion != InspectionConclusion.Failed;
        }
        private async Task<OrderActionDto> BuildOrderActionsAsync(
            OrderDetailDto detail,
            agreement_form agreement,
            bool isBuyer,
            bool isSeller,
            CancellationToken ct)
        {
            var canConfirm = false;
            OrderConfirmAction? confirmAction = null;

            var now = DateTime.UtcNow;

            var inspectionCollectNow =
                await IsInspectionCollectNowReadyAsync(
                    detail.OrderId,
                    ct);

            var latestCollection =
                detail.Appointments
                    .Where(x =>
                        x.AppointmentType ==
                        AppointmentType.Collection)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

            var isDirect =
                detail.DeliveryMethod ==
                    DeliveryMethod.BuyerPickUp ||
                detail.DeliveryMethod ==
                    DeliveryMethod.SellerDelivers;

            var collectionConfirmationOpen =
                latestCollection?.ScheduledAt.HasValue == true &&
                (
                    latestCollection.AppointmentStatus ==
                        AppointmentStatus.Scheduled ||
                    latestCollection.AppointmentStatus ==
                        AppointmentStatus.InProgress
                ) &&
                IsCollectionConfirmationOpen(
                    latestCollection.ScheduledAt.Value,
                    now);

            var ghnDelivered =
                detail.DeliveryMethod ==
                    DeliveryMethod.GhnDelivery &&
                detail.Shipment?.ShipmentStatus ==
                    ShipmentStatus.Delivered &&
                detail.Shipment.DeliveredAt.HasValue;

            if (detail.OrderStatus == OrderStatus.Processing)
            {
                if (
                    isSeller &&
                    (
                        inspectionCollectNow ||
                        (
                            isDirect &&
                            collectionConfirmationOpen
                        )
                    ) &&
                    !detail.SellerHandoverConfirmedAt.HasValue)
                {
                    canConfirm = true;

                    confirmAction =
                        OrderConfirmAction.ConfirmHandover;
                }
                else if (
                    isBuyer &&
                    !detail.BuyerReceivedConfirmedAt.HasValue &&
                    (
                        inspectionCollectNow ||
                        (
                            isDirect &&
                            collectionConfirmationOpen
                        ) ||
                        ghnDelivered
                    ))
                {
                    canConfirm = true;

                    confirmAction =
                        OrderConfirmAction.ConfirmReceived;
                }
            }

            var canReview =
                detail.OrderStatus ==
                    OrderStatus.Completed &&
                !detail.Review.HasReviewed;

            var canDispute = false;

            if (!detail.Dispute.HasActiveDispute)
            {
                if (detail.OrderStatus == OrderStatus.Processing)
                {
                    canDispute = true;
                }
                else if (detail.OrderStatus == OrderStatus.Completed)
                {
                    DateTime? disputeWindowEndsAt =
                        detail.DisputeWindowEndsAt;

                    // Fallback cho Order cũ được tạo trước Phase 4B,
                    // chưa có snapshot DisputeWindowEndsAt.
                    if (!disputeWindowEndsAt.HasValue)
                    {
                        var disputeWindowStart =
                            detail.Shipment?.DeliveredAt ??
                            detail.CompletedAt;

                        if (disputeWindowStart.HasValue)
                        {
                            var disputeWindow =
                                await _disputeWindowPolicy
                                    .GetOrderDisputeWindowAsync(
                                        agreement.SellerId,
                                        ct);

                            disputeWindowEndsAt =
                                disputeWindowStart.Value
                                    .Add(disputeWindow);
                        }
                    }

                    canDispute =
                        disputeWindowEndsAt.HasValue &&
                        now <= disputeWindowEndsAt.Value;
                }
            }

            var latestInspectionForm =
                await _inspectionFormRepo.GetLatestByOrderIdAsync(
                    detail.OrderId,
                    ct);

            var canCancel =
                detail.OrderStatus == OrderStatus.Processing &&
                !detail.Dispute.HasActiveDispute &&
                latestInspectionForm?.InspectionStatus ==
                    (int)InspectionStatus.Rejected;

            return new OrderActionDto
            {
                CanConfirm = canConfirm,
                ConfirmAction = confirmAction,
                CanCancel = canCancel,
                CanReview = canReview,
                CanDispute = canDispute,

                AllowedDisputeCategories =
                    canDispute
                        ? BuildAllowedDisputeCategories(detail)
                        : Array.Empty<DisputeCategory>()
            };
        }

        private static IReadOnlyList<DisputeCategory> BuildAllowedDisputeCategories(OrderDetailDto detail)
        {
            var categories = new List<DisputeCategory>();

            if (detail.Appointments.Count > 0)
                categories.Add(DisputeCategory.NoShow);

            categories.Add(DisputeCategory.ItemMismatch);

            if (detail.DeliveryMethod == DeliveryMethod.GhnDelivery)
            {
                categories.Add(DisputeCategory.SellerNotShipped);
                categories.Add(DisputeCategory.DamagedOrLost);
                categories.Add(DisputeCategory.ItemNotReceived);
            }

            categories.Add(DisputeCategory.FraudOrScam);
            categories.Add(DisputeCategory.PaymentNotCompleted);
            categories.Add(DisputeCategory.CommitmentViolation);
            categories.Add(DisputeCategory.Other);

            return categories;
        }

        private static Result<DeliveryMethod> ResolveDeliveryMethod(agreement_form agreement)
        {
            if (string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);

            try
            {
                var details = JsonSerializer.Deserialize<AgreementDetailsDto>(
                    agreement.AgreementDetailsJsonb,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (details?.DeliveryMethod == null ||
                    details.DeliveryMethod == DeliveryMethod.Unknown)
                {
                    return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);
                }

                return Result<DeliveryMethod>.Success(details.DeliveryMethod.Value);
            }
            catch (JsonException)
            {
                return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);
            }
        }
        private async Task<Result<bool>> CheckOwnershipAsync(Guid agreementId, Guid userId, CancellationToken ct)
        {
            var agreement = await _agreementRepo.GetByIdAsync(
                agreementId,
                ct);

            if (agreement == null)
                return Result<bool>.Fail(AgreementErrors.NotFound);

            if (agreement.BuyerId != userId &&
                agreement.SellerId != userId)
            {
                return Result<bool>.Fail(OrderErrors.Forbidden);
            }

            return Result<bool>.Success(true);
        }

        private static bool IsCollectionConfirmationOpen(DateTime scheduledAt, DateTime nowUtc)
        {
            var scheduledUtc = scheduledAt.Kind == DateTimeKind.Utc
                ? scheduledAt
                : DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc);

            var now = nowUtc.Kind == DateTimeKind.Utc
                ? nowUtc
                : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

            var scheduledVietnamDate = DateOnly.FromDateTime(scheduledUtc.AddHours(7));
            var currentVietnamDate = DateOnly.FromDateTime(now.AddHours(7));

            return currentVietnamDate >= scheduledVietnamDate;
        }
    }
}
