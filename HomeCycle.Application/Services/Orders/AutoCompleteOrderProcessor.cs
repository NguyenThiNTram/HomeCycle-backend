using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public sealed class AutoCompleteOrderProcessor : IOrderLifecycleProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IDisputeWindowPolicy _disputeWindowPolicy;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AutoCompleteOrderProcessor> _logger;

        public string Name => "AutoCompleteOrder";

        public AutoCompleteOrderProcessor(
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository,
            IShipmentRepository shipmentRepository,
            IDisputeRepository disputeRepository,
            IDisputeWindowPolicy disputeWindowPolicy,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IUnitOfWork unitOfWork,
            ILogger<AutoCompleteOrderProcessor> logger)
        {
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
            _shipmentRepository = shipmentRepository;
            _disputeRepository = disputeRepository;
            _disputeWindowPolicy = disputeWindowPolicy;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken ct = default)
        {
            var policy = await _platformPolicyProvider.GetOrderConfigAsync(ct);

            var cutoffUtc = DateTime.UtcNow.AddHours(
                -policy.BuyerReceiveConfirmationTimeoutHours);

            var candidateIds =
                await _orderRepository.GetAutoCompleteCandidateIdsAsync(
                    cutoffUtc,
                    batchSize,
                    ct);

            var processed = 0;

            foreach (var orderId in candidateIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await ProcessOneAsync(
                        orderId,
                        policy.BuyerReceiveConfirmationTimeoutHours,
                        ct))
                    {
                        processed++;
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Auto-complete failed for Order {OrderId}.",
                        orderId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            return processed;
        }

        private async Task<bool> ProcessOneAsync(
            Guid orderId,
            int timeoutHours,
            CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order =
                    await _orderRepository.GetByIdForUpdateAsync(
                        orderId,
                        ct);

                if (order == null ||
                    order.OrderStatus != (int)OrderStatus.Processing ||
                    order.BuyerReceivedConfirmedAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var hasActiveDispute =
                    await _disputeRepository.ExistsActiveAsync(
                        DisputeTargetType.Order,
                        order.OrderId,
                        ct);

                if (hasActiveDispute)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var agreement =
                    await _agreementRepository.GetByIdAsync(
                        order.AgreementId,
                        ct);

                if (agreement == null)
                {
                    throw new InvalidOperationException(
                        $"Agreement {order.AgreementId} was not found for Order {order.OrderId}.");
                }

                var shipment =
                    await _shipmentRepository.GetByOrderIdAsync(
                        order.OrderId,
                        ct);

                if (shipment == null)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                DateTime? confirmationWindowStartedAt =
                    shipment.DeliveryMethod switch
                    {
                        DeliveryMethod.GhnDelivery
                            when shipment.ShipmentStatus == ShipmentStatus.Delivered
                                 && shipment.DeliveredAt.HasValue
                            => shipment.DeliveredAt,

                        DeliveryMethod.SellerDelivers
                            or DeliveryMethod.BuyerPickUp
                            when order.SellerHandoverConfirmedAt.HasValue
                            => order.SellerHandoverConfirmedAt,

                        _ => null
                    };

                if (!confirmationWindowStartedAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var now = DateTime.UtcNow;
                var confirmationDeadline =
                    confirmationWindowStartedAt.Value.AddHours(timeoutHours);

                if (confirmationDeadline > now)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var disputeWindow =
                    await _disputeWindowPolicy.GetOrderDisputeWindowAsync(
                        agreement.SellerId,
                        ct);

                order.OrderStatus = (int)OrderStatus.Completed;
                order.PaymentStatus = (int)PaymentStatus.Completed;
                order.CompletedAt = now;
                order.CompletionSource =
                    (int)OrderCompletionSource.AutoConfirmed;

                order.DisputeWindowEndsAt ??=
                    now.Add(disputeWindow);

                order.UpdatedAt = now;

                await _orderRepository.UpdateAsync(order, ct);

                var notification =
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Đơn hàng đã tự động hoàn thành",
                            "Thời hạn xác nhận nhận hàng đã kết thúc. Đơn hàng đã được hệ thống tự động chuyển sang trạng thái hoàn thành.",
                            NotificationTargetType.Order,
                            order.OrderId),
                        ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                await _notificationService.PublishCreatedSafelyAsync(
                    notification);

                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId,
                    order.UpdatedAt);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }
    }
}
