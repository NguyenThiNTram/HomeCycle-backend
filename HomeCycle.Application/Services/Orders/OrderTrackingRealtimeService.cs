using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
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
        private readonly IOrderTrackingRealtimePublisher _realtimePublisher;
        private readonly ILogger<OrderTrackingRealtimeService> _logger;

        public OrderTrackingRealtimeService(
            IOrderRepository orderRepository,
            IOrderTrackingRealtimePublisher realtimePublisher,
            ILogger<OrderTrackingRealtimeService> logger)
        {
            _orderRepository = orderRepository;
            _realtimePublisher = realtimePublisher;
            _logger = logger;
        }

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

                await PublishCoreAsync(order.OrderId, updatedAt, timeout.Token);
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
                await PublishCoreAsync(orderId, updatedAt, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát OrderTrackingUpdated cho Order {OrderId}.",
                    orderId);
            }
        }

        private Task PublishCoreAsync(Guid orderId, DateTime updatedAt, CancellationToken cancellationToken)
        {
            return _realtimePublisher.PublishUpdatedAsync(
                orderId,
                new OrderTrackingUpdatedResponse
                {
                    OrderId = orderId,
                    UpdatedAt = updatedAt
                },
                cancellationToken);
        }
    }
}
