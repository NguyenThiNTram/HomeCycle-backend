using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public sealed class OrderTrackingRealtimeService : IOrderTrackingRealtimeService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IGhnShipmentRepository _ghnShipmentRepository;
        private readonly IOrderTrackingRealtimePublisher _realtimePublisher;
        private readonly ILogger<OrderTrackingRealtimeService> _logger;

        public OrderTrackingRealtimeService(
            IOrderRepository orderRepository,
            IGhnShipmentRepository ghnShipmentRepository,
            IOrderTrackingRealtimePublisher realtimePublisher,
            ILogger<OrderTrackingRealtimeService> logger)
        {
            _orderRepository = orderRepository;
            _ghnShipmentRepository = ghnShipmentRepository;
            _realtimePublisher = realtimePublisher;
            _logger = logger;
        }

        // Các hàm Publish* được gọi sau khi transaction đã commit. Tham số updatedAt của nơi gọi được giữ để
        // không đổi interface; UpdatedAt của event là thời điểm đọc snapshot nên luôn tăng theo thứ tự dữ liệu.
        public Task PublishByOrderIdSafelyAsync(Guid orderId, DateTime updatedAt)
        {
            return PublishSafelyAsync(orderId, updatedAt);
        }

        public async Task PublishByAgreementIdSafelyAsync(Guid agreementId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var order = await _orderRepository.GetByAgreementIdAsync(agreementId, timeout.Token);

                if (order == null)
                {
                    _logger.LogWarning(
                        "Không thể phát OrderTrackingUpdated vì không tìm thấy Order của Agreement {AgreementId}.",
                        agreementId);
                    return;
                }

                await PublishCoreAsync(order.OrderId, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát OrderTrackingUpdated cho Agreement {AgreementId}.",
                    agreementId);
            }
        }

        private async Task PublishSafelyAsync(Guid orderId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await PublishCoreAsync(orderId, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát OrderTrackingUpdated cho Order {OrderId}.",
                    orderId);
            }
        }

        private async Task PublishCoreAsync(Guid orderId, CancellationToken cancellationToken)
        {
            var snapshotAt = DateTime.UtcNow;
            var snapshot = await BuildSnapshotAsync(orderId, cancellationToken);

            await _realtimePublisher.PublishUpdatedAsync(
                orderId,
                new OrderTrackingUpdatedResponse
                {
                    OrderId = orderId,
                    UpdatedAt = snapshotAt,
                    Order = snapshot
                },
                cancellationToken);
        }

        // Đọc từ cùng truy vấn với GET /orders/{id} rồi chỉ giữ trạng thái chung, nên không lặp logic
        // chọn vận chuyển/khiếu nại mới nhất. Không dựng timeline và không tính Can*.
        private async Task<OrderRealtimeSnapshotDto?> BuildSnapshotAsync(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            try
            {
                // currentUserId chỉ ảnh hưởng thông tin đối tác (Counterparty), không dùng ở đây.
                var detail = await _orderRepository.GetDetailWithRelationsAsync(
                    orderId,
                    Guid.Empty,
                    cancellationToken);

                if (detail == null)
                    return null;

                var ghnShipment = detail.DeliveryMethod == DeliveryMethod.GhnDelivery
                    ? await _ghnShipmentRepository.GetByOrderIdAsync(orderId, cancellationToken)
                    : null;

                return new OrderRealtimeSnapshotDto
                {
                    OrderStatus = detail.OrderStatus?.ToString(),
                    PaymentStatus = detail.PaymentStatus?.ToString(),
                    DeliveryMethod = detail.DeliveryMethod?.ToString(),

                    FinalTotalAmount = detail.FinalTotalAmount,
                    AmountPaid = detail.AmountPaid,
                    AmountRemaining = detail.AmountRemaining,

                    SellerHandoverConfirmedAt = detail.SellerHandoverConfirmedAt,
                    BuyerReceivedConfirmedAt = detail.BuyerReceivedConfirmedAt,
                    CompletedAt = detail.CompletedAt,
                    CompletionSource = detail.CompletionSource?.ToString(),
                    DisputeWindowEndsAt = detail.DisputeWindowEndsAt,

                    ReturnDueAt = detail.ReturnDueAt,
                    BuyerReturnConfirmedAt = detail.BuyerReturnConfirmedAt,
                    SellerReturnReceivedAt = detail.SellerReturnReceivedAt,
                    ReturnedAt = detail.ReturnedAt,

                    Cancellation = detail.Cancellation,

                    Shipment = detail.Shipment == null
                        ? null
                        : new OrderRealtimeShipmentDto
                        {
                            ShipmentId = detail.Shipment.ShipmentId,
                            ShipmentStatus = detail.Shipment.ShipmentStatus?.ToString(),
                            SellerReadyAt = detail.Shipment.SellerReadyAt,
                            PickedUpAt = detail.Shipment.PickedUpAt,
                            DeliveredAt = detail.Shipment.DeliveredAt,
                            TrackingCode = ghnShipment?.GHNOrderCode,
                            CarrierStatus = ghnShipment?.GHNStatusCode,
                            CreationStatus = ghnShipment?.CreationStatus.ToString(),
                            ExpectedDeliveryAt = ghnShipment?.ExpectedDeliveryAt
                        },

                    Dispute = new OrderRealtimeDisputeDto
                    {
                        HasActiveDispute = detail.Dispute.HasActiveDispute,
                        LatestDisputeId = detail.Dispute.LatestDisputeId,
                        LatestDisputeStatus = detail.Dispute.LatestDisputeStatus?.ToString(),
                        LatestDisputeCreatedAt = detail.Dispute.LatestDisputeCreatedAt,
                        LatestDisputeResolvedAt = detail.Dispute.LatestDisputeResolvedAt
                    },

                    UpdatedAt = detail.UpdatedAt
                };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Không dựng được snapshot Order {OrderId}; chỉ phát tín hiệu tải lại.",
                    orderId);
                return null;
            }
        }
    }
}
