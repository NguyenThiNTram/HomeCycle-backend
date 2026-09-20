using HomeCycle.Domain.Enums;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Notifications;
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
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Application.Services.Disputes;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.GHN;

namespace HomeCycle.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        private const decimal AmountEpsilon = 0.01m;
        private readonly IOrderRepository _orderRepo;
        private readonly IPostRepository _postRepo;
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
        private readonly IDisputeCategoryRepository _disputeCategoryRepo;
        private readonly IPaymentService _paymentService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IOrderTimelineBuilder _orderTimelineBuilder;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly IGhnShipmentCreationService _ghnLifecycle;

        public OrderService(
            IOrderRepository orderRepo,
            IPostRepository postRepo,
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
            IDisputeCategoryRepository disputeCategoryRepo,
            IPaymentService paymentService,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IOrderTimelineBuilder orderTimelineBuilder,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IAuditService auditService,
            IMapper mapper,
            IGhnShipmentCreationService ghnLifecycle)
        {
            _orderRepo = orderRepo;
            _postRepo = postRepo;
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
            _disputeCategoryRepo = disputeCategoryRepo;
            _paymentService = paymentService;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _orderTimelineBuilder = orderTimelineBuilder;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _auditService = auditService;
            _mapper = mapper;
            _ghnLifecycle = ghnLifecycle;
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

            var inspectionCollectNow =
                await IsInspectionCollectNowReadyAsync(
                    detail.OrderId,
                    ct);

            detail.Timeline =
                _orderTimelineBuilder.Build(
                    detail,
                    inspectionCollectNow);



            detail.Actions = await BuildOrderActionsAsync(
                detail,
                agreement,
                isBuyer,
                isSeller,
                inspectionCollectNow,
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

                var shipment = await _shipmentRepo.GetByOrderIdAsync(order.OrderId, ct);

                if (shipment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(
                        OrderErrors.ShipmentNotFound);
                }

                appointment? lockedCollection = null;

                if (!inspectionCollectNow)
                {
                    var deliveryMethodResult = ResolveDeliveryMethod(agreement, shipment);

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

                    if (!shipment.SellerReadyAt.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(
                            OrderErrors.SellerReadyRequired);
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
                notification? handoverNotification = null;
                AuditEvent? handoverAuditEvent = null;

                if (inspectionCollectNow && !shipment.SellerReadyAt.HasValue)
                {
                    shipment.SellerReadyAt = confirmedAt;
                    shipment.UpdatedAt = confirmedAt;

                    await _shipmentRepo.UpdateAsync(shipment, ct);

                    changed = true;
                }

                if (!order.SellerHandoverConfirmedAt.HasValue)
                {
                    order.SellerHandoverConfirmedAt = confirmedAt;
                    order.UpdatedAt = confirmedAt;

                    var handoverAuditDiff = new AuditDiffBuilder()
                        .Add(
                            "sellerHandoverConfirmed",
                            false,
                            true);

                    handoverAuditEvent = new AuditEvent
                    {
                        Category = AuditCategory.BusinessOperation,
                        Action = AuditActions.OrderHandoverConfirm,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.User,
                        UserId = sellerId,
                        TargetType = AuditTargetTypes.Order,
                        TargetId = order.OrderId,
                        OldValues = handoverAuditDiff.OldValues,
                        NewValues = handoverAuditDiff.NewValues,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["shipmentId"] = shipment.ShipmentId,
                            ["inspectionCollectNow"] = inspectionCollectNow
                        }
                    };

                    await _orderRepo.UpdateAsync(order, ct);

                    handoverNotification = await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Người bán đã giao hàng",
                            "Người bán đã xác nhận bàn giao sản phẩm. Vui lòng kiểm tra và xác nhận khi đã nhận hàng.",
                            NotificationTargetType.Order,
                            order.OrderId),
                        ct);

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

                if (handoverAuditEvent != null)
                {
                    await _auditService.EnqueueAsync(
                        handoverAuditEvent,
                        ct);
                }


                if (changed)
                    await _unitOfWork.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                if (handoverNotification != null)
                    await _notificationService.PublishCreatedSafelyAsync(handoverNotification);

                if (changed)
                {
                    await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                        order.OrderId,
                        order.UpdatedAt);
                }

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
                var shipment = await _shipmentRepo.GetByOrderIdAsync(order.OrderId, ct);

                if (!inspectionCollectNow)
                {
                    var deliveryMethodResult = ResolveDeliveryMethod(agreement, shipment);

                    if (!deliveryMethodResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);

                        return Result<OrderConfirmationResponseDto>.Fail(
                            deliveryMethodResult.Error!);
                    }

                    var deliveryMethod = deliveryMethodResult.Data;

                    if (deliveryMethod == DeliveryMethod.GhnDelivery)
                    {

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

                var previousOrderStatus =
                    (OrderStatus)order.OrderStatus;

                var previousPaymentStatus =
                    order.PaymentStatus.HasValue
                        ? (PaymentStatus?)order.PaymentStatus.Value
                        : null;

                var previousBuyerReceivedConfirmed =
                    order.BuyerReceivedConfirmedAt.HasValue;

                var previousCompletionSource =
                    order.CompletionSource.HasValue
                        ? (OrderCompletionSource?)order.CompletionSource.Value
                        : null;

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

                var completeOrderAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousOrderStatus.ToString(),
                        OrderStatus.Completed.ToString())
                    .Add(
                        "paymentStatus",
                        previousPaymentStatus?.ToString(),
                        PaymentStatus.Completed.ToString())
                    .Add(
                        "buyerReceivedConfirmed",
                        previousBuyerReceivedConfirmed,
                        true)
                    .Add(
                        "completionSource",
                        previousCompletionSource?.ToString(),
                        OrderCompletionSource.BuyerConfirmed.ToString());

                var completeOrderAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.OrderComplete,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = buyerId,
                    TargetType = AuditTargetTypes.Order,
                    TargetId = order.OrderId,
                    OldValues = completeOrderAuditDiff.OldValues,
                    NewValues = completeOrderAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["disputeWindowEndsAt"] =
                            order.DisputeWindowEndsAt
                    }
                };

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

                var receivedNotification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        agreement.SellerId,
                        "Đơn hàng đã hoàn thành",
                        "Người mua đã xác nhận nhận hàng. Đơn hàng đã chuyển sang trạng thái hoàn thành.",
                        NotificationTargetType.Order,
                        order.OrderId),
                    ct);
                await _auditService.EnqueueAsync(
                    completeOrderAuditEvent,
                    ct);


                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                await _notificationService.PublishCreatedSafelyAsync(receivedNotification);

                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId,
                    order.UpdatedAt);

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

        public async Task<Result<OrderCancellationResponseDto>> CancelOrderAsync(
            Guid orderId,
            Guid userId,
            CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(orderId, ct);
                if (tradeSnapshot != null)
                    await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, ct);

                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != userId && agreement.SellerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                if (order.OrderStatus == (int)OrderStatus.Cancelled && order.CancelledAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderCancellationResponseDto>.Success(new OrderCancellationResponseDto
                    {
                        OrderId = order.OrderId,
                        OrderStatus = OrderStatus.Cancelled,
                        PaymentStatus = order.PaymentStatus.HasValue ? (PaymentStatus?)order.PaymentStatus.Value : null,
                        CancelledAt = order.CancelledAt.Value,
                        CancelledByUserId = order.CancelledByUserId,
                        CancellationReason = order.CancellationReason
                    });
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(OrderErrors.InvalidStatus);
                }

                var hasActiveDispute = await _disputeRepo.ExistsActiveAsync(
                    DisputeTargetType.Order,
                    order.OrderId,
                    ct);

                if (hasActiveDispute)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(OrderErrors.ActiveDisputeBlocksCancellation);
                }

                var now = DateTime.UtcNow;

                appointment? appointmentToCancel = null;
                shipment? shipmentToCancel = null;

                if (agreement.AgreementType == (int)AgreementType.Inspection)
                {
                    var appointmentSnapshot = await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                        agreement.AgreementId,
                        AppointmentType.Inspection,
                        ct);

                    if (appointmentSnapshot == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(AppointmentErrors.NotFound);
                    }

                    appointmentToCancel = await _appointmentRepo.GetByIdForUpdateAsync(
                        appointmentSnapshot.AppointmentId,
                        ct);

                    if (appointmentToCancel == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(AppointmentErrors.NotFound);
                    }

                    var inspectionOverdue =
                        appointmentToCancel.LateThresholdAt.HasValue &&
                        now >= appointmentToCancel.LateThresholdAt.Value;

                    var canCancelInspection =
                        appointmentToCancel.AppointmentStatus == (int)AppointmentStatus.Scheduled &&
                        !appointmentToCancel.BuyerCheckAt.HasValue &&
                        !appointmentToCancel.SellerCheckAt.HasValue &&
                        !inspectionOverdue;

                    if (!canCancelInspection)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(OrderErrors.CancellationNotAllowed);
                    }
                }
                else if (agreement.AgreementType == (int)AgreementType.No_Inspection)
                {
                    var appointmentSnapshot = await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                        agreement.AgreementId,
                        AppointmentType.Collection,
                        ct);

                    if (appointmentSnapshot == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(AppointmentErrors.NotFound);
                    }

                    appointmentToCancel = await _appointmentRepo.GetByIdForUpdateAsync(
                        appointmentSnapshot.AppointmentId,
                        ct);

                    if (appointmentToCancel == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(AppointmentErrors.NotFound);
                    }

                    var shipmentSnapshot = await _shipmentRepo.GetByOrderIdAsync(order.OrderId, ct);

                    if (shipmentSnapshot == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(OrderErrors.ShipmentNotFound);
                    }

                    shipmentToCancel = await _shipmentRepo.GetByIdForUpdateAsync(
                        shipmentSnapshot.ShipmentId,
                        ct);

                    if (shipmentToCancel == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(OrderErrors.ShipmentNotFound);
                    }

                    var isDirectCollection =
                        shipmentToCancel.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                        shipmentToCancel.DeliveryMethod == DeliveryMethod.SellerDelivers;

                    var collectionOverdue =
                        isDirectCollection &&
                        appointmentToCancel.LateThresholdAt.HasValue &&
                        now >= appointmentToCancel.LateThresholdAt.Value;

                    var canCancelCollection =
                        appointmentToCancel.AppointmentStatus == (int)AppointmentStatus.Scheduled &&
                        shipmentToCancel.ShipmentStatus == ShipmentStatus.ReadyToPick &&
                        !shipmentToCancel.SellerReadyAt.HasValue &&
                        !shipmentToCancel.PickedUpAt.HasValue &&
                        !collectionOverdue;

                    if (!canCancelCollection)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderCancellationResponseDto>.Fail(OrderErrors.CancellationNotAllowed);
                    }
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(OrderErrors.CancellationNotAllowed);
                }

                var refundResult = await _paymentService.RefundAllRemainingOrderHeldAmountAsync(
                    order,
                    agreement,
                    ct);

                if (!refundResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderCancellationResponseDto>.Fail(refundResult.Error!);
                }

                var refundedAmount = refundResult.Data;
                var remainingPaid = Math.Max((order.AmountPaid ?? 0) - refundedAmount, 0);
                var paymentStatus = remainingPaid <= AmountEpsilon
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                var cancellationReason = agreement.AgreementType == (int)AgreementType.Inspection
                    ? "Transaction cancelled before inspection started."
                    : "Transaction cancelled before seller confirmed readiness.";

                appointmentToCancel.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                appointmentToCancel.CancelledAt = now;
                appointmentToCancel.CancellationReason = cancellationReason;
                appointmentToCancel.UpdatedAt = now;

                await _appointmentRepo.UpdateAsync(appointmentToCancel, ct);

                var proposalSnapshot = await _appointmentRepo.GetPendingRescheduleProposalAsync(
                    appointmentToCancel.AppointmentId,
                    ct);

                if (proposalSnapshot != null)
                {
                    var proposal = await _appointmentRepo.GetByIdForUpdateAsync(
                        proposalSnapshot.AppointmentId,
                        ct);

                    if (proposal?.AppointmentStatus == (int)AppointmentStatus.Proposed)
                    {
                        proposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;
                        proposal.CancelledAt = now;
                        proposal.CancellationReason = cancellationReason;
                        proposal.UpdatedAt = now;

                        await _appointmentRepo.UpdateAsync(proposal, ct);
                    }
                }

                if (shipmentToCancel != null)
                {
                    shipmentToCancel.ShipmentStatus = ShipmentStatus.Cancelled;
                    shipmentToCancel.UpdatedAt = now;

                    await _shipmentRepo.UpdateAsync(shipmentToCancel, ct);
                }

                await _postRepo.RestoreOrderQuantityAsync(order.OrderId, true, ct);

                var previousOrderStatus = (OrderStatus)order.OrderStatus.Value;
                var previousPaymentStatus = order.PaymentStatus.HasValue
                    ? (PaymentStatus?)order.PaymentStatus.Value
                    : null;

                order.OrderStatus = (int)OrderStatus.Cancelled;
                order.PaymentStatus = (int)paymentStatus;
                order.AmountPaid = remainingPaid;
                order.AmountRemaining = 0;
                order.CancelledAt = now;
                order.CancelledByUserId = userId;
                order.CancellationReason = cancellationReason;
                order.DisputeWindowEndsAt = null;
                order.UpdatedAt = now;

                await _orderRepo.UpdateAsync(order, ct);

                var cancelOrderAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousOrderStatus.ToString(), OrderStatus.Cancelled.ToString())
                    .Add("paymentStatus", previousPaymentStatus?.ToString(), paymentStatus.ToString());

                var cancelOrderAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.OrderCancel,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Order,
                    TargetId = order.OrderId,
                    OldValues = cancelOrderAuditDiff.OldValues,
                    NewValues = cancelOrderAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["agreementType"] = ((AgreementType)agreement.AgreementType).ToString(),
                        ["deliveryMethod"] = shipmentToCancel?.DeliveryMethod.ToString(),
                        ["refundedAmount"] = refundedAmount
                    }
                };

                var cancelRecipientId = userId == agreement.BuyerId
                    ? agreement.SellerId
                    : agreement.BuyerId;

                var isGhnDelivery = shipmentToCancel?.DeliveryMethod == DeliveryMethod.GhnDelivery;
                var notificationMessage = isGhnDelivery
                    ? "Đơn hàng đã bị hủy. Khoản tiền hàng nền tảng đang tạm giữ đã được hoàn lại; phí vận chuyển GHN không được hoàn."
                    : "Đơn hàng đã bị hủy. Khoản tiền nền tảng đang tạm giữ đã được hoàn lại.";

                var cancellationNotification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        cancelRecipientId,
                        "Đơn hàng đã bị hủy",
                        notificationMessage,
                        NotificationTargetType.Order,
                        order.OrderId),
                    ct);

                await _auditService.EnqueueAsync(cancelOrderAuditEvent, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                await _notificationService.PublishCreatedSafelyAsync(cancellationNotification);
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(order.OrderId, order.UpdatedAt);

                if (isGhnDelivery)
                    await _ghnLifecycle.CancelForOrderSafelyAsync(order.OrderId, CancellationToken.None);

                return Result<OrderCancellationResponseDto>.Success(new OrderCancellationResponseDto
                {
                    OrderId = order.OrderId,
                    OrderStatus = OrderStatus.Cancelled,
                    PaymentStatus = paymentStatus,
                    CancelledAt = now,
                    CancelledByUserId = userId,
                    CancellationReason = cancellationReason
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<OrderReturnConfirmationResponseDto>> ConfirmReturnByBuyerAsync(
            Guid orderId,
            Guid buyerId,
            CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var dispute = await _disputeRepo.GetAwaitingReturnByOrderIdForUpdateAsync(orderId, ct);

                if (dispute == null)
                {
                    var existingOrder = await _orderRepo.GetByIdAsync(orderId, ct);

                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return existingOrder == null
                        ? Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.NotFound)
                        : Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                if (dispute.ResolutionOutcome != (int)DisputeResolutionOutcome.BuyerFavored)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.BuyerId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                if (order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                if (order.BuyerReturnConfirmedAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    var existingResponse = _mapper.Map<OrderReturnConfirmationResponseDto>(order);
                    existingResponse.DisputeId = dispute.DisputeId;
                    existingResponse.DisputeStatus = DisputeStatus.AwaitingReturn;

                    return Result<OrderReturnConfirmationResponseDto>.Success(existingResponse);
                }

                if (!order.ReturnDueAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                var now = DateTime.UtcNow;

                if (now > order.ReturnDueAt.Value)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(
                        OrderErrors.ReturnDeadlineExpired(order.ReturnDueAt.Value));
                }

                var policy = await _platformPolicyProvider.GetDisputeConfigAsync(ct);

                var previousBuyerReturnConfirmed = order.BuyerReturnConfirmedAt.HasValue;
                var previousReturnDueAt = order.ReturnDueAt;
                order.BuyerReturnConfirmedAt = now;
                order.ReturnDueAt = now.AddDays(policy.ReturnWindowDays);
                order.UpdatedAt = now;

                var confirmReturnAuditDiff = new AuditDiffBuilder()
                    .Add("buyerReturnConfirmed", previousBuyerReturnConfirmed, true)
                    .Add("returnDueAt", previousReturnDueAt, order.ReturnDueAt);

                var confirmReturnAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.OrderReturnConfirm,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = buyerId,
                    TargetType = AuditTargetTypes.Order,
                    TargetId = order.OrderId,
                    OldValues = confirmReturnAuditDiff.OldValues,
                    NewValues = confirmReturnAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["disputeId"] = dispute.DisputeId
                    }
                };

                dispute.UpdatedAt = now;

                await _orderRepo.UpdateAsync(order, ct);
                await _disputeRepo.UpdateAsync(dispute, ct);
                var returnNotification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        agreement.SellerId,
                        "Người mua đã trả hàng",
                        "Người mua đã xác nhận gửi trả sản phẩm. Vui lòng xác nhận sau khi nhận được hàng.",
                        NotificationTargetType.Order,
                        order.OrderId),
                    ct);
                await _auditService.EnqueueAsync(confirmReturnAuditEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                await _notificationService.PublishCreatedSafelyAsync(returnNotification);
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId,
                    order.UpdatedAt);

                var response = _mapper.Map<OrderReturnConfirmationResponseDto>(order);
                response.DisputeId = dispute.DisputeId;
                response.DisputeStatus = DisputeStatus.AwaitingReturn;
                response.RefundedAmount = 0;

                return Result<OrderReturnConfirmationResponseDto>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<OrderReturnConfirmationResponseDto>> ConfirmReturnReceivedBySellerAsync(
            Guid orderId,
            Guid sellerId,
            CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var tradeSnapshot = await _postRepo.GetTradeByOrderAsync(orderId, ct);
                if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, ct);

                var dispute = await _disputeRepo.GetAwaitingReturnByOrderIdForUpdateAsync(orderId, ct);

                if (dispute == null)
                {
                    var existingOrder = await _orderRepo.GetByIdAsync(orderId, ct);

                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return existingOrder == null
                        ? Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.NotFound)
                        : Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                if (dispute.ResolutionOutcome != (int)DisputeResolutionOutcome.BuyerFavored)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                if (order.OrderStatus != (int)OrderStatus.Disputing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(OrderErrors.ReturnConfirmationNotAllowed);
                }

                var previousOrderStatus = (OrderStatus)order.OrderStatus.Value;
                var previousPaymentStatus = order.PaymentStatus.HasValue
                    ? (PaymentStatus?)order.PaymentStatus.Value
                    : null;
                var previousSellerReturnReceived = order.SellerReturnReceivedAt.HasValue;
                var previousReturnDueAt = order.ReturnDueAt;

                var refundResult = await _paymentService.RefundAllRemainingOrderHeldAmountAsync(
                    order,
                    agreement,
                    ct);

                if (!refundResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderReturnConfirmationResponseDto>.Fail(refundResult.Error!);
                }

                var now = DateTime.UtcNow;
                var refundedAmount = refundResult.Data;
                var remainingPaid = Math.Max((order.AmountPaid ?? 0) - refundedAmount, 0);

                order.AmountPaid = remainingPaid;
                order.AmountRemaining = 0;
                order.PaymentStatus = remainingPaid <= AmountEpsilon
                    ? (int)PaymentStatus.Refunded
                    : (int)PaymentStatus.PartiallyRefunded;
                await _postRepo.RestoreOrderQuantityAsync(order.OrderId, true, ct);
                order.OrderStatus = (int)OrderStatus.Returned;
                order.SellerReturnReceivedAt = now;
                order.ReturnDueAt = null;
                order.ReturnedAt = now;
                order.UpdatedAt = now;

                dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                var completeReturnAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousOrderStatus.ToString(), ((OrderStatus)order.OrderStatus.Value).ToString())
                    .Add("paymentStatus", previousPaymentStatus?.ToString(), order.PaymentStatus.HasValue ? ((PaymentStatus)order.PaymentStatus.Value).ToString() : null)
                    .Add("sellerReturnReceived", previousSellerReturnReceived, true)
                    .Add("returnDueAt", previousReturnDueAt, order.ReturnDueAt);

                var completeReturnAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.OrderReturnComplete,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = sellerId,
                    TargetType = AuditTargetTypes.Order,
                    TargetId = order.OrderId,
                    OldValues = completeReturnAuditDiff.OldValues,
                    NewValues = completeReturnAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["disputeId"] = dispute.DisputeId,
                        ["disputeStatus"] = DisputeStatus.Resolved.ToString()
                    }
                };

                await _orderRepo.UpdateAsync(order, ct);
                await _disputeRepo.UpdateAsync(dispute, ct);

                var returnNotification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        agreement.BuyerId,
                        "Hoàn trả đã hoàn tất",
                        "Người bán đã xác nhận nhận lại sản phẩm. Khoản tiền nền tảng giữ đã được hoàn lại cho bạn.",
                        NotificationTargetType.Order,
                        order.OrderId),
                    ct);
                await _auditService.EnqueueAsync(completeReturnAuditEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                await _notificationService.PublishCreatedSafelyAsync(returnNotification);
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId,
                    order.UpdatedAt);

                var response = _mapper.Map<OrderReturnConfirmationResponseDto>(order);
                response.DisputeId = dispute.DisputeId;
                response.DisputeStatus = DisputeStatus.Resolved;
                response.RefundedAmount = refundedAmount;

                return Result<OrderReturnConfirmationResponseDto>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }


        public async Task<Result<PagedResult<ModeratorOrderListItemDto>>> GetAllForModeratorAsync(
            ModeratorOrderSearchRequest request,
            CancellationToken ct = default)
        {
            var readResult =
                await _orderRepo.GetPagedForModeratorAsync(
                    new ModeratorOrderQuery
                    {
                        Keyword = request.Keyword,

                        Status = request.Status,
                        PaymentStatus = request.PaymentStatus,

                        BuyerId = request.BuyerId,
                        SellerId = request.SellerId,

                        HasActiveDispute = request.HasActiveDispute,
                        HasInspection = request.HasInspection,

                        CreatedFrom = request.CreatedFrom,
                        CreatedTo = request.CreatedTo,

                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    },
                    ct);

            var items = readResult.Items
                .Select(x =>
                    new ModeratorOrderListItemDto
                    {
                        OrderId = x.OrderId,
                        OrderCode = x.OrderCode,
                        ProductName = x.ProductName,
                        ThumbnailUrl = x.ThumbnailUrl,

                        Quantity = x.Quantity,

                        FinalTotalAmount = x.FinalTotalAmount,
                        AmountPaid = x.AmountPaid,
                        AmountRemaining = x.AmountRemaining,

                        OrderStatus =
                            x.OrderStatus.HasValue
                                ? (OrderStatus?)x.OrderStatus.Value
                                : null,

                        PaymentStatus =
                            x.PaymentStatus.HasValue
                                ? (PaymentStatus?)x.PaymentStatus.Value
                                : null,

                        Buyer =
                            new ModeratorOrderPartyDto
                            {
                                UserId = x.BuyerId,
                                Username = x.BuyerUsername,
                                PhoneNumber = x.BuyerPhoneNumber,
                                AvatarUrl = x.BuyerAvatarUrl
                            },

                        Seller =
                            new ModeratorOrderPartyDto
                            {
                                UserId = x.SellerId,
                                Username = x.SellerUsername,
                                PhoneNumber = x.SellerPhoneNumber,
                                AvatarUrl = x.SellerAvatarUrl
                            },

                        HasActiveDispute =
                            x.HasActiveDispute,

                        LatestDisputeId =
                            x.LatestDisputeId,

                        LatestDisputeStatus =
                            x.LatestDisputeStatus.HasValue
                                ? (DisputeStatus?)
                                    x.LatestDisputeStatus.Value
                                : null,

                        HasInspection =
                            x.HasInspection,

                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                .ToList();

            return Result<PagedResult<ModeratorOrderListItemDto>>
                .Success(
                    new PagedResult<ModeratorOrderListItemDto>
                    {
                        Items = items,
                        PageNumber = readResult.PageNumber,
                        PageSize = readResult.PageSize,
                        TotalCount = readResult.TotalCount
                    });
        }

        public async Task<Result<ModeratorOrderDetailDto>> GetDetailForModeratorAsync(
            Guid orderId,
            CancellationToken ct = default)
        {
            var context =
                await _orderRepo.GetForModeratorAsync(
                    orderId,
                    ct);

            if (context == null)
            {
                return Result<ModeratorOrderDetailDto>
                    .Fail(OrderErrors.NotFound);
            }

            // Legacy detail repository cần currentUserId chỉ để tính Counterparty.
            // Moderator DTO không sử dụng Counterparty nên truyền BuyerId là an toàn.
            var detail =
                await _orderRepo.GetDetailWithRelationsAsync(
                    orderId,
                    context.BuyerId,
                    ct);

            if (detail == null)
            {
                return Result<ModeratorOrderDetailDto>
                    .Fail(OrderErrors.NotFound);
            }

            detail.Appointments =
                await _appointmentRepo
                    .GetAppointmentSummariesByAgreementIdAsync(
                        detail.AgreementId,
                        ct);

            var inspectionCollectNow =
                await IsInspectionCollectNowReadyAsync(
                    detail.OrderId,
                    ct);

            detail.Timeline =
                _orderTimelineBuilder.Build(
                    detail,
                    inspectionCollectNow);

            return Result<ModeratorOrderDetailDto>
                .Success(
                    new ModeratorOrderDetailDto
                    {
                        OrderId = detail.OrderId,
                        AgreementId = detail.AgreementId,
                        PostId = detail.PostId,

                        OrderCode = detail.OrderCode,
                        ProductName = detail.ProductName,
                        Quantity = detail.Quantity,

                        OriginalTotalAmount =
                            detail.OriginalTotalAmount,

                        FinalTotalAmount =
                            detail.FinalTotalAmount,

                        AmountPaid =
                            detail.AmountPaid,

                        AmountRemaining =
                            detail.AmountRemaining,

                        ShippingFee =
                            detail.ShippingFee,

                        PaymentStatus =
                            detail.PaymentStatus,

                        OrderStatus =
                            detail.OrderStatus,

                        DeliveryMethod =
                            detail.DeliveryMethod,

                        CreatedAt =
                            detail.CreatedAt,

                        UpdatedAt =
                            detail.UpdatedAt,

                        CompletedAt =
                            detail.CompletedAt,

                        SellerHandoverConfirmedAt =
                            detail.SellerHandoverConfirmedAt,

                        BuyerReceivedConfirmedAt =
                            detail.BuyerReceivedConfirmedAt,

                        CompletionSource =
                            detail.CompletionSource,

                        BuyerReturnConfirmedAt =
                            detail.BuyerReturnConfirmedAt,

                        SellerReturnReceivedAt =
                            detail.SellerReturnReceivedAt,

                        ReturnDueAt =
                            detail.ReturnDueAt,

                        ReturnedAt =
                            detail.ReturnedAt,

                        DisputeWindowEndsAt =
                            detail.DisputeWindowEndsAt,

                        ThumbnailUrl =
                            detail.ThumbnailUrl,

                        PostDescription =
                            detail.PostDescription,

                        Cancellation =
                            detail.Cancellation,

                        Buyer =
                            new ModeratorOrderPartyDto
                            {
                                UserId = context.BuyerId,
                                Username = context.BuyerUsername,
                                PhoneNumber = context.BuyerPhoneNumber,
                                AvatarUrl = context.BuyerAvatarUrl
                            },

                        Seller =
                            new ModeratorOrderPartyDto
                            {
                                UserId = context.SellerId,
                                Username = context.SellerUsername,
                                PhoneNumber = context.SellerPhoneNumber,
                                AvatarUrl = context.SellerAvatarUrl
                            },

                        Payment =
                            detail.Payment,

                        Shipment =
                            detail.Shipment,

                        Appointments =
                            detail.Appointments,

                        Dispute =
                            detail.Dispute,

                        Timeline =
                            detail.Timeline
                    });
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

            if (!form.Conclusion.HasValue)
                return false;
            var conclusion = (InspectionConclusion)form.Conclusion.Value;

            return conclusion != InspectionConclusion.Failed;
        }
        private async Task<OrderActionDto> BuildOrderActionsAsync(
            OrderDetailDto detail,
            agreement_form agreement,
            bool isBuyer,
            bool isSeller,
            bool inspectionCollectNow,
            CancellationToken ct)
        {
            var canConfirm = false;
            OrderConfirmAction? confirmAction = null;

            var now = DateTime.UtcNow;

            var shipment = await _shipmentRepo.GetByOrderIdAsync(detail.OrderId, ct);

            var supportsSellerReady =
                shipment != null &&
                (shipment.DeliveryMethod == DeliveryMethod.GhnDelivery ||
                 shipment.DeliveryMethod == DeliveryMethod.SellerDelivers ||
                 shipment.DeliveryMethod == DeliveryMethod.BuyerPickUp);

            var canConfirmSellerReady =
                isSeller &&
                !inspectionCollectNow &&
                detail.OrderStatus == OrderStatus.Processing &&
                supportsSellerReady &&
                shipment!.ShipmentStatus == ShipmentStatus.ReadyToPick &&
                !shipment.SellerReadyAt.HasValue;


            var latestCollection =
                detail.Appointments
                    .Where(x =>
                        x.AppointmentType ==
                        AppointmentType.Collection)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

            var latestInspection = detail.Appointments
                .Where(x => x.AppointmentType == AppointmentType.Inspection)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var isDirect =
                detail.DeliveryMethod ==
                    DeliveryMethod.BuyerPickUp ||
                detail.DeliveryMethod ==
                    DeliveryMethod.SellerDelivers;

            var inspectionStarted =
                latestInspection?.InspectionCheckIn?.BuyerCheckAt.HasValue == true ||
                latestInspection?.InspectionCheckIn?.SellerCheckAt.HasValue == true;

            var inspectionNoShowEligible =
                latestInspection != null &&
                latestInspection.AppointmentStatus is AppointmentStatus.Scheduled or AppointmentStatus.InProgress &&
                latestInspection.LateThresholdAt.HasValue &&
                now >= latestInspection.LateThresholdAt.Value &&
                (latestInspection.InspectionCheckIn?.BuyerCheckAt == null ||
                 latestInspection.InspectionCheckIn?.SellerCheckAt == null);

            var directCollectionNoShowEligible =
                isDirect &&
                latestCollection != null &&
                latestCollection.AppointmentStatus is AppointmentStatus.Scheduled or AppointmentStatus.InProgress &&
                latestCollection.LateThresholdAt.HasValue &&
                now >= latestCollection.LateThresholdAt.Value;

            var noShowEligible =
                agreement.AgreementType == (int)AgreementType.Inspection
                    ? inspectionNoShowEligible
                    : agreement.AgreementType == (int)AgreementType.No_Inspection && directCollectionNoShowEligible;

            var deliveryStarted =
                shipment != null &&
                (shipment.SellerReadyAt.HasValue ||
                 shipment.PickedUpAt.HasValue ||
                 (shipment.ShipmentStatus.HasValue &&
                  shipment.ShipmentStatus != ShipmentStatus.ReadyToPick));

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
                            collectionConfirmationOpen &&
                            shipment?.SellerReadyAt.HasValue == true
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
                    canDispute = agreement.AgreementType == (int)AgreementType.Inspection
                        ? inspectionStarted || inspectionNoShowEligible
                        : agreement.AgreementType == (int)AgreementType.No_Inspection &&
                          (deliveryStarted || directCollectionNoShowEligible);
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
                            detail.CompletedAt ??
                            detail.Shipment?.DeliveredAt; ;

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

            var canCancelInspection =
                agreement.AgreementType == (int)AgreementType.Inspection &&
                latestInspection?.AppointmentStatus == AppointmentStatus.Scheduled &&
                latestInspection.InspectionCheckIn?.BuyerCheckAt == null &&
                latestInspection.InspectionCheckIn?.SellerCheckAt == null &&
                !inspectionNoShowEligible;

            var canCancelCollection =
                agreement.AgreementType == (int)AgreementType.No_Inspection &&
                latestCollection?.AppointmentStatus == AppointmentStatus.Scheduled &&
                shipment?.ShipmentStatus == ShipmentStatus.ReadyToPick &&
                !shipment.SellerReadyAt.HasValue &&
                !shipment.PickedUpAt.HasValue &&
                !directCollectionNoShowEligible;

            var canCancel =
                detail.OrderStatus == OrderStatus.Processing &&
                !detail.Dispute.HasActiveDispute &&
                (canCancelInspection || canCancelCollection);


            var isAwaitingReturn =
                detail.OrderStatus == OrderStatus.Disputing &&
                detail.Dispute.LatestDisputeStatus == DisputeStatus.AwaitingReturn;

            var canConfirmReturn =
                isBuyer &&
                isAwaitingReturn &&
                !detail.BuyerReturnConfirmedAt.HasValue &&
                detail.ReturnDueAt.HasValue &&
                now <= detail.ReturnDueAt.Value;

            var canConfirmReturnReceived =
                isSeller &&
                isAwaitingReturn &&
                !detail.SellerReturnReceivedAt.HasValue;

            var allowedDisputeCategories =
                canDispute
                    ? await BuildAllowedDisputeCategoriesAsync(detail, noShowEligible, ct)
                    : Array.Empty<DisputeCategoryOptionDto>();

            return new OrderActionDto
            {
                CanConfirmSellerReady = canConfirmSellerReady,
                CanConfirm = canConfirm,
                ConfirmAction = confirmAction,
                CanCancel = canCancel,
                CanReview = canReview,
                CanDispute = canDispute,
                CanConfirmReturn = canConfirmReturn,
                CanConfirmReturnReceived = canConfirmReturnReceived,
                AllowedDisputeCategories = allowedDisputeCategories
            };
        }

        private async Task<IReadOnlyList<DisputeCategoryOptionDto>> BuildAllowedDisputeCategoriesAsync(
            OrderDetailDto detail,
            bool noShowEligible,
            CancellationToken ct)
        {
            var categories =
                await _disputeCategoryRepo
                    .GetActiveByTargetTypeAsync(
                        DisputeTargetType.Order,
                        ct);

            return categories
                .Where(x =>
                    OrderDisputeCategoryPolicy.IsAllowed(
                        x.Code,
                        noShowEligible,
                        detail.DeliveryMethod))
                .Select(x =>
                    _mapper.Map<DisputeCategoryOptionDto>(x))
                .ToArray();
        }

        private static Result<DeliveryMethod> ResolveDeliveryMethod(agreement_form agreement, shipment? shipment = null)
        {
            if (shipment != null && shipment.DeliveryMethod != DeliveryMethod.Unknown)
                return Result<DeliveryMethod>.Success(shipment.DeliveryMethod);

            if (string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);

            try
            {
                var details = JsonSerializer.Deserialize<AgreementDetailsDto>(
                    agreement.AgreementDetailsJsonb,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var deliveryMethod = details?.DeliveryMethod;

                if (!deliveryMethod.HasValue || deliveryMethod.Value == DeliveryMethod.Unknown)
                    return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);

                return Result<DeliveryMethod>.Success(deliveryMethod.Value);
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
