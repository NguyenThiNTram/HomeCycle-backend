using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public sealed class BuyerReturnTimeoutProcessor : IOrderLifecycleProcessor
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly INotificationService _notificationService;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BuyerReturnTimeoutProcessor> _logger;

        public string Name => "BuyerReturnTimeout";

        public BuyerReturnTimeoutProcessor(
            IDisputeRepository disputeRepository,
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository,
            INotificationService notificationService,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IUnitOfWork unitOfWork,
            ILogger<BuyerReturnTimeoutProcessor> logger)
        {
            _disputeRepository = disputeRepository;
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
            _notificationService = notificationService;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken ct = default)
        {
            var candidateIds =
                await _disputeRepository.GetBuyerReturnTimeoutCandidateIdsAsync(
                    DateTime.UtcNow,
                    batchSize,
                    ct);

            var processed = 0;

            foreach (var disputeId in candidateIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await ProcessOneAsync(disputeId, ct))
                        processed++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Buyer return timeout failed for Dispute {DisputeId}.",
                        disputeId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            return processed;
        }

        private async Task<bool> ProcessOneAsync(
            Guid disputeId,
            CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var dispute =
                    await _disputeRepository.GetByIdForUpdateAsync(
                        disputeId,
                        ct);

                if (dispute == null ||
                    dispute.DisputeStatus != (int)DisputeStatus.AwaitingReturn ||
                    dispute.ResolutionOutcome !=
                        (int)DisputeResolutionOutcome.BuyerFavored ||
                    !dispute.OrderId.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var order =
                    await _orderRepository.GetByIdForUpdateAsync(
                        dispute.OrderId.Value,
                        ct);

                if (order == null ||
                    order.OrderStatus != (int)OrderStatus.Disputing ||
                    !order.CompletedAt.HasValue ||
                    order.BuyerReturnConfirmedAt.HasValue ||
                    order.SellerReturnReceivedAt.HasValue ||
                    !order.ReturnDueAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return false;
                }

                var now = DateTime.UtcNow;

                if (order.ReturnDueAt.Value >= now)
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

                order.OrderStatus = (int)OrderStatus.Completed;
                order.ReturnDueAt = null;
                order.UpdatedAt = now;

                dispute.DisputeStatus = (int)DisputeStatus.Resolved;
                dispute.ResolvedAt = now;
                dispute.UpdatedAt = now;

                await _orderRepository.UpdateAsync(order, ct);
                await _disputeRepository.UpdateAsync(dispute, ct);

                var notifications = new List<notification>
                {
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Thời hạn hoàn trả đã kết thúc",
                            "Bạn chưa xác nhận hoàn trả sản phẩm trong thời hạn quy định. Đơn hàng đã được khôi phục về trạng thái hoàn thành.",
                            NotificationTargetType.Order,
                            order.OrderId),
                        ct),

                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Thời hạn hoàn trả đã kết thúc",
                            "Người mua chưa xác nhận hoàn trả sản phẩm trong thời hạn quy định. Đơn hàng đã được khôi phục về trạng thái hoàn thành.",
                            NotificationTargetType.Order,
                            order.OrderId),
                        ct)
                };

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                foreach (var notification in notifications)
                {
                    await _notificationService.PublishCreatedSafelyAsync(
                        notification);
                }

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
