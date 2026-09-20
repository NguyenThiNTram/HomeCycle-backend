using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class OrderDisputeTargetHandler : IDisputeTargetHandler
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IDisputeWindowPolicy _windowPolicy;

        public DisputeTargetType TargetType => DisputeTargetType.Order;

        public OrderDisputeTargetHandler(
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository,
            IShipmentRepository shipmentRepository,
            IAppointmentRepository appointmentRepository,
            IDisputeRepository disputeRepository,
            IDisputeWindowPolicy windowPolicy)
        {
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
            _shipmentRepository = shipmentRepository;
            _appointmentRepository = appointmentRepository;
            _disputeRepository = disputeRepository;
            _windowPolicy = windowPolicy;
        }

        public async Task<Result<DisputeTargetCreateContext>> PrepareCreateAsync(
            Guid senderId,
            Guid targetId,
            string categoryCode,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            // Lock Order trước để serialize các thao tác thay đổi business state trên cùng Order.
            var order = await _orderRepository.GetByIdForUpdateAsync(targetId, cancellationToken);

            if (order == null)
                return Result<DisputeTargetCreateContext>.Fail(OrderErrors.NotFound);

            var agreement = await _agreementRepository.GetByIdAsync(order.AgreementId, cancellationToken);

            if (agreement == null)
                return Result<DisputeTargetCreateContext>.Fail(AgreementErrors.NotFound);

            var isBuyer = agreement.BuyerId == senderId;
            var isSeller = agreement.SellerId == senderId;

            if (!isBuyer && !isSeller)
                return Result<DisputeTargetCreateContext>.Fail(OrderErrors.Forbidden);

            var orderStatus = order.OrderStatus.HasValue
                ? (OrderStatus?)order.OrderStatus.Value
                : null;

            if (orderStatus != OrderStatus.Processing && orderStatus != OrderStatus.Completed)
                return Result<DisputeTargetCreateContext>.Fail(OrderErrors.InvalidStatus);

            var appointments = await _appointmentRepository.GetAppointmentSummariesByAgreementIdAsync(
                order.AgreementId,
                cancellationToken);

            var shipment = await _shipmentRepository.GetByOrderIdAsync(order.OrderId, cancellationToken);

            DeliveryMethod? deliveryMethod = shipment?.DeliveryMethod;

            if (!deliveryMethod.HasValue || deliveryMethod.Value == DeliveryMethod.Unknown)
                deliveryMethod = TryResolveDeliveryMethod(agreement);

            var latestInspection = appointments
                .Where(x => x.AppointmentType == AppointmentType.Inspection)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var latestCollection = appointments
                .Where(x => x.AppointmentType == AppointmentType.Collection)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            var inspectionStarted =
                latestInspection?.InspectionCheckIn?.BuyerCheckAt.HasValue == true ||
                latestInspection?.InspectionCheckIn?.SellerCheckAt.HasValue == true;

            var inspectionNoShowEligible =
                latestInspection != null &&
                latestInspection.AppointmentStatus is AppointmentStatus.Scheduled or AppointmentStatus.InProgress &&
                latestInspection.LateThresholdAt.HasValue &&
                nowUtc >= latestInspection.LateThresholdAt.Value &&
                (latestInspection.InspectionCheckIn?.BuyerCheckAt == null ||
                 latestInspection.InspectionCheckIn?.SellerCheckAt == null);

            var isDirectCollection =
                deliveryMethod == DeliveryMethod.BuyerPickUp ||
                deliveryMethod == DeliveryMethod.SellerDelivers;

            var directCollectionNoShowEligible =
                isDirectCollection &&
                latestCollection != null &&
                latestCollection.AppointmentStatus is AppointmentStatus.Scheduled or AppointmentStatus.InProgress &&
                latestCollection.LateThresholdAt.HasValue &&
                nowUtc >= latestCollection.LateThresholdAt.Value;

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

            if (orderStatus == OrderStatus.Processing)
            {
                var canDispute = agreement.AgreementType == (int)AgreementType.Inspection
                    ? inspectionStarted || inspectionNoShowEligible
                    : agreement.AgreementType == (int)AgreementType.No_Inspection &&
                      (deliveryStarted || directCollectionNoShowEligible);

                if (!canDispute)
                    return Result<DisputeTargetCreateContext>.Fail(OrderErrors.InvalidStatus);
            }

            if (!OrderDisputeCategoryPolicy.IsAllowed(categoryCode, noShowEligible, deliveryMethod))
                return Result<DisputeTargetCreateContext>.Fail(DisputeErrors.InvalidCategory(categoryCode));

            // Order đã được lock trước khi check duplicate nên 2 request tạo dispute song song
            // trên cùng Order không thể cùng thay đổi state thành công.
            var hasActiveDispute = await _disputeRepository.ExistsActiveAsync(
                DisputeTargetType.Order,
                order.OrderId,
                cancellationToken);

            if (hasActiveDispute)
                return Result<DisputeTargetCreateContext>.Fail(DisputeErrors.DuplicateActiveDispute);

            DateTime? disputeDeadlineUtc = null;

            if (orderStatus == OrderStatus.Completed)
            {
                // Ưu tiên snapshot đã lưu trên Order.
                disputeDeadlineUtc = order.DisputeWindowEndsAt;

                // Fallback chỉ dành cho dữ liệu Order legacy chưa có snapshot.
                if (!disputeDeadlineUtc.HasValue)
                {
                    DateTime? fallbackWindowStart = order.CompletedAt;

                    if (!fallbackWindowStart.HasValue)
                    {
                        fallbackWindowStart = shipment?.DeliveredAt;
                    }

                    if (!fallbackWindowStart.HasValue)
                        return Result<DisputeTargetCreateContext>.Fail(OrderErrors.InvalidCompletionState);

                    var disputeWindow = await _windowPolicy.GetOrderDisputeWindowAsync(
                        agreement.SellerId,
                        cancellationToken);

                    disputeDeadlineUtc = fallbackWindowStart.Value.Add(disputeWindow);
                }

                if (nowUtc > disputeDeadlineUtc.Value)
                    return Result<DisputeTargetCreateContext>.Fail(
                        DisputeErrors.WindowExpired(disputeDeadlineUtc.Value));
            }

            var targetUserId = isBuyer ? agreement.SellerId : agreement.BuyerId;

            // Freeze transaction trong thời gian dispute Pending.
            // CompletedAt vẫn được giữ để có thể restore về Completed khi Close Dispute.
            order.OrderStatus = (int)OrderStatus.Disputing;
            order.UpdatedAt = nowUtc;

            await _orderRepository.UpdateAsync(order, cancellationToken);

            return Result<DisputeTargetCreateContext>.Success(new DisputeTargetCreateContext
            {
                TargetType = DisputeTargetType.Order,
                TargetId = order.OrderId,
                TargetUserId = targetUserId,
                OrderId = order.OrderId,
                ReviewId = null,
                DisputeDeadlineUtc = disputeDeadlineUtc
            });
        }

        public async Task<Result<DisputeTargetSummaryDto>> BuildSummaryAsync(
            dispute dispute,
            CancellationToken cancellationToken = default)
        {
            if (!dispute.OrderId.HasValue)
            {
                return Result<DisputeTargetSummaryDto>.Fail(
                    new Error("DISPUTE_ORDER_MISSING", "Tranh chấp không có OrderId."));
            }

            var order = await _orderRepository.GetByIdAsync(dispute.OrderId.Value, cancellationToken);

            if (order == null)
                return Result<DisputeTargetSummaryDto>.Fail(OrderErrors.NotFound);

            var agreement = await _agreementRepository.GetByIdAsync(order.AgreementId, cancellationToken);

            if (agreement == null)
                return Result<DisputeTargetSummaryDto>.Fail(AgreementErrors.NotFound);

            var shipment = await _shipmentRepository.GetByOrderIdAsync(order.OrderId, cancellationToken);

            var windowStart = order.CompletedAt ?? shipment?.DeliveredAt;
            var deadline = order.DisputeWindowEndsAt;

            int windowHours;

            if (deadline.HasValue && windowStart.HasValue)
            {
                windowHours = Math.Max(
                    0,
                    (int)Math.Round((deadline.Value - windowStart.Value).TotalHours));
            }
            else
            {
                var disputeWindow = await _windowPolicy.GetOrderDisputeWindowAsync(
                    agreement.SellerId,
                    cancellationToken);

                windowHours = Math.Max(0, (int)Math.Round(disputeWindow.TotalHours));

                // Fallback cho Order legacy chưa snapshot.
                if (!deadline.HasValue && windowStart.HasValue)
                    deadline = windowStart.Value.Add(disputeWindow);
            }

            var orderSummary = new OrderDisputeSummaryDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                PostId = order.PostId,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                FinalTotalAmount = order.FinalTotalAmount,
                OrderStatus = order.OrderStatus.HasValue ? (OrderStatus?)order.OrderStatus.Value : null,
                PaymentStatus = order.PaymentStatus.HasValue ? (PaymentStatus?)order.PaymentStatus.Value : null,
                CompletedAt = order.CompletedAt,
                DeliveredAt = shipment?.DeliveredAt,
                BuyerReturnConfirmedAt = order.BuyerReturnConfirmedAt,
                SellerReturnReceivedAt = order.SellerReturnReceivedAt,
                ReturnDueAt = order.ReturnDueAt,
                ReturnedAt = order.ReturnedAt,
                DisputeDeadlineUtc = deadline,
                DisputeWindowHours = windowHours
            };

            return Result<DisputeTargetSummaryDto>.Success(new DisputeTargetSummaryDto
            {
                TargetType = DisputeTargetType.Order,
                TargetId = order.OrderId,
                Order = orderSummary
            });
        }

        private static DeliveryMethod? TryResolveDeliveryMethod(agreement_form agreement)
        {
            if (string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                return null;

            try
            {
                var details = JsonSerializer.Deserialize<AgreementDetailsDto>(
                    agreement.AgreementDetailsJsonb,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var deliveryMethod = details?.DeliveryMethod;

                if (!deliveryMethod.HasValue || deliveryMethod.Value == DeliveryMethod.Unknown)
                    return null;

                return deliveryMethod.Value;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
