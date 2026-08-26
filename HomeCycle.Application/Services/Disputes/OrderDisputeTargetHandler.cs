using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class OrderDisputeTargetHandler
        : IDisputeTargetHandler
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository
            _agreementRepository;
        private readonly IShipmentRepository
            _shipmentRepository;
        private readonly IDisputeRepository
            _disputeRepository;
        private readonly IDisputeWindowPolicy
            _windowPolicy;

        public DisputeTargetType TargetType
            => DisputeTargetType.Order;

        public OrderDisputeTargetHandler(
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository,
            IShipmentRepository shipmentRepository,
            IDisputeRepository disputeRepository,
            IDisputeWindowPolicy windowPolicy)
        {
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
            _shipmentRepository = shipmentRepository;
            _disputeRepository = disputeRepository;
            _windowPolicy = windowPolicy;
        }

        public async Task<
            Result<DisputeTargetCreateContext>>
            PrepareCreateAsync(
                Guid senderId,
                Guid targetId,
                DisputeCategory category,
                DateTime nowUtc,
                CancellationToken cancellationToken = default)
        {
            // Quan trọng:
            // lock Order trước để tránh 2 request tạo dispute song song.
            var order =
                await _orderRepository
                    .GetByIdForUpdateAsync(
                        targetId,
                        cancellationToken);

            if (order == null)
                return Result<DisputeTargetCreateContext>.Fail(DisputeErrors.OrderNotFound);

            var agreement =
                await _agreementRepository
                    .GetByIdAsync(
                        order.AgreementId,
                        cancellationToken);

            if (agreement == null)
                return Result<DisputeTargetCreateContext>.Fail(DisputeErrors.AgreementNotFound);

            // Chỉ Buyer hoặc Seller của Order mới được dispute.
            bool isBuyer =
                agreement.BuyerId == senderId;

            bool isSeller =
                agreement.SellerId == senderId;

            if (!isBuyer && !isSeller)
            {
                return Result<DisputeTargetCreateContext>
                    .Fail(DisputeErrors.Forbidden);
            }

            if (!IsValidOrderCategory(category))
            {
                return Result<DisputeTargetCreateContext>
                    .Fail(
                        DisputeErrors.InvalidCategory(
                            category));
            }

            // Sau khi đã lock Order mới kiểm tra duplicate.
            var hasActiveDispute =
                await _disputeRepository
                    .ExistsActiveAsync(
                        DisputeTargetType.Order,
                        order.OrderId,
                        cancellationToken);

            if (hasActiveDispute)
            {
                return Result<DisputeTargetCreateContext>
                    .Fail(
                        DisputeErrors
                            .DuplicateActiveDispute);
            }

            var status =
                order.OrderStatus.HasValue
                    ? (OrderStatus?)
                        order.OrderStatus.Value
                    : null;

            // Chỉ cho phép trong quá trình giao dịch
            // hoặc khoảng grace period sau giao hàng/completed.
            if (status != OrderStatus.Processing &&
                status != OrderStatus.Completed)
            {
                return Result<DisputeTargetCreateContext>
                    .Fail(
                        DisputeErrors
                            .InvalidOrderStatus);
            }

            var shipment =
                await _shipmentRepository
                    .GetByOrderIdAsync(
                        order.OrderId,
                        cancellationToken);

            /*
             * Requirement lấy mốc giao nhận thành công.
             *
             * Ưu tiên Shipment.DeliveredAt.
             *
             * Với pickup / trường hợp không có shipment DeliveredAt,
             * fallback Order.CompletedAt.
             */
            DateTime? disputeWindowStart =
                shipment?.DeliveredAt
                ?? order.CompletedAt;

            DateTime? disputeDeadlineUtc = null;

            /*
             * Nếu Order vẫn Processing và chưa giao xong
             * thì dispute được phép tạo,
             * chưa có deadline hậu giao dịch.
             *
             * Nếu đã có DeliveredAt dù Order vẫn Processing,
             * grace period đã bắt đầu.
             */
            if (disputeWindowStart.HasValue)
            {
                var window =
                    await _windowPolicy
                        .GetOrderDisputeWindowAsync(
                            agreement.SellerId,
                            cancellationToken);

                disputeDeadlineUtc =
                    disputeWindowStart.Value
                        .Add(window);

                if (nowUtc >
                    disputeDeadlineUtc.Value)
                {
                    return Result<
                        DisputeTargetCreateContext>
                        .Fail(
                            DisputeErrors
                                .WindowExpired(
                                    disputeDeadlineUtc
                                        .Value));
                }
            }
            else if (status ==
                     OrderStatus.Completed)
            {
                /*
                 * Completed mà không có DeliveredAt
                 * cũng không có CompletedAt
                 * là trạng thái dữ liệu không hợp lệ.
                 */
                return Result<
                    DisputeTargetCreateContext>
                    .Fail(
                        DisputeErrors
                            .InvalidCompletionState);
            }

            /*
             * Backend tự suy ra người bị khiếu nại.
             */
            Guid targetUserId =
                isBuyer
                    ? agreement.SellerId
                    : agreement.BuyerId;

            /*
             * Có dispute -> freeze business state.
             *
             * Completed cũng được chuyển lại Disputing.
             * Không xóa CompletedAt vì cần giữ audit/mốc thời gian.
             */
            order.OrderStatus =
                (int)OrderStatus.Disputing;

            order.UpdatedAt = nowUtc;

            await _orderRepository.UpdateAsync(
                order,
                cancellationToken);

            return Result<
                DisputeTargetCreateContext>
                .Success(
                    new DisputeTargetCreateContext
                    {
                        TargetType =
                            DisputeTargetType.Order,

                        TargetId =
                            order.OrderId,

                        TargetUserId =
                            targetUserId,

                        OrderId =
                            order.OrderId,

                        ReviewId =
                            null,

                        DisputeDeadlineUtc =
                            disputeDeadlineUtc
                    });
        }

        public async Task<
            Result<DisputeTargetSummaryDto>>
            BuildSummaryAsync(
                dispute dispute,
                CancellationToken cancellationToken = default)
        {
            if (!dispute.OrderId.HasValue)
            {
                return Result<
                    DisputeTargetSummaryDto>
                    .Fail(
                        new Error(
                            "DISPUTE_ORDER_MISSING",
                            "Tranh chấp không có OrderId."));
            }

            var order =
                await _orderRepository
                    .GetByIdAsync(
                        dispute.OrderId.Value,
                        cancellationToken);

            if (order == null)
            {
                return Result<
                    DisputeTargetSummaryDto>
                    .Fail(
                        new Error(
                            "ORDER_NOT_FOUND",
                            "Không tìm thấy đơn hàng."));
            }

            var agreement =
                await _agreementRepository
                    .GetByIdAsync(
                        order.AgreementId,
                        cancellationToken);

            var shipment =
                await _shipmentRepository
                    .GetByOrderIdAsync(
                        order.OrderId,
                        cancellationToken);

            DateTime? windowStart =
                shipment?.DeliveredAt
                ?? order.CompletedAt;

            DateTime? deadline = null;

            int windowHours = 72;

            if (windowStart.HasValue &&
                agreement != null)
            {
                var window =
                    await _windowPolicy
                        .GetOrderDisputeWindowAsync(
                            agreement.SellerId,
                            cancellationToken);

                windowHours =
                    (int)window.TotalHours;

                deadline =
                    windowStart.Value.Add(window);
            }

            var orderSummary =
                new OrderDisputeSummaryDto
                {
                    OrderId =
                        order.OrderId,

                    OrderCode =
                        order.OrderCode,

                    PostId =
                        order.PostId,

                    ProductName =
                        order.ProductName,

                    Quantity =
                        order.Quantity,

                    FinalTotalAmount =
                        order.FinalTotalAmount,

                    OrderStatus =
                        order.OrderStatus.HasValue
                            ? (OrderStatus?)
                                order.OrderStatus.Value
                            : null,

                    PaymentStatus =
                        order.PaymentStatus.HasValue
                            ? (PaymentStatus?)
                                order.PaymentStatus.Value
                            : null,

                    CompletedAt =
                        order.CompletedAt,

                    DeliveredAt =
                        shipment?.DeliveredAt,

                    DisputeDeadlineUtc =
                        deadline,

                    DisputeWindowHours =
                        windowHours
                };

            return Result<
                DisputeTargetSummaryDto>
                .Success(
                    new DisputeTargetSummaryDto
                    {
                        TargetType =
                            DisputeTargetType.Order,

                        TargetId =
                            order.OrderId,

                        Order =
                            orderSummary
                    });
        }

        private static bool IsValidOrderCategory(
            DisputeCategory category)
        {
            return category is
                DisputeCategory.NoShow
                or DisputeCategory.ItemMismatch
                or DisputeCategory.SellerNotShipped
                or DisputeCategory.DamagedOrLost
                or DisputeCategory.ItemNotReceived
                or DisputeCategory.FraudOrScam
                or DisputeCategory.PaymentNotCompleted
                or DisputeCategory.CommitmentViolation
                or DisputeCategory.Other;
        }
    }
}
