using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public sealed class OrderTimelineBuilder : IOrderTimelineBuilder
    {
        public IReadOnlyList<OrderTimelineStepDto> Build(OrderDetailDto detail, bool isInspectionCollectNow)
        {
            ArgumentNullException.ThrowIfNull(detail);

            var steps = new List<OrderTimelineStepDto>
            {
                CreateStep(
                    "order_created",
                    "Đơn hàng đã được tạo",
                    "Thanh toán đã được ghi nhận và đơn hàng đã được khởi tạo.",
                    OrderTimelineStepStatus.Completed,
                    detail.CreatedAt)
            };

            var inspection = GetEffectiveAppointment(detail.Appointments, AppointmentType.Inspection);
            var collection = GetEffectiveAppointment(detail.Appointments, AppointmentType.Collection);

            if (inspection != null)
                steps.Add(BuildInspectionStep(inspection));

            var isDirectDelivery =
                detail.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                detail.DeliveryMethod == DeliveryMethod.SellerDelivers;

            if (isDirectDelivery && !isInspectionCollectNow)
                steps.Add(BuildCollectionScheduleStep(collection));

            if (detail.OrderStatus == OrderStatus.Cancelled)
            {
                if (detail.Shipment?.SellerReadyAt.HasValue == true)
                    steps.Add(BuildSellerReadyStep(detail, true));

                if (HasFulfillmentProgress(detail))
                    steps.Add(BuildFulfillmentStep(detail, isInspectionCollectNow));

                steps.Add(CreateStep(
                    "order_cancelled",
                    "Đơn hàng đã bị hủy",
                    detail.Cancellation?.Reason,
                    OrderTimelineStepStatus.Cancelled,
                    detail.Cancellation?.CancelledAt));

                return steps;
            }

            var inspectionCompleted =
                inspection == null ||
                inspection.AppointmentStatus == AppointmentStatus.Completed;

            var collectionScheduled =
                !isDirectDelivery ||
                isInspectionCollectNow ||
                collection?.AppointmentStatus is AppointmentStatus.Scheduled
                    or AppointmentStatus.InProgress
                    or AppointmentStatus.Completed;

            var sellerReadyPrerequisitesCompleted =
                inspectionCompleted && collectionScheduled;

            if (detail.OrderStatus != OrderStatus.Returned ||
                detail.Shipment?.SellerReadyAt.HasValue == true)
            {
                steps.Add(BuildSellerReadyStep(
                    detail,
                    sellerReadyPrerequisitesCompleted));
            }

            OrderTimelineStepDto? fulfillmentStep = null;

            if (detail.OrderStatus != OrderStatus.Returned ||
                HasFulfillmentProgress(detail))
            {
                fulfillmentStep = BuildFulfillmentStep(
                    detail,
                    isInspectionCollectNow);

                steps.Add(fulfillmentStep);
            }

            var disputeAfterCompletion =
                detail.CompletedAt.HasValue &&
                detail.Dispute.LatestDisputeCreatedAt.HasValue &&
                detail.Dispute.LatestDisputeCreatedAt.Value >= detail.CompletedAt.Value;

            if (detail.Dispute.LatestDisputeId.HasValue && !disputeAfterCompletion)
                steps.Add(BuildDisputeStep(detail));

            if (detail.OrderStatus != OrderStatus.Returned ||
                detail.CompletedAt.HasValue)
            {
                steps.Add(BuildCompletionStep(detail, fulfillmentStep));
            }

            if (detail.Dispute.LatestDisputeId.HasValue && disputeAfterCompletion)
                steps.Add(BuildDisputeStep(detail));

            if (ShouldShowReturnStep(detail))
                steps.Add(BuildReturnStep(detail));

            return steps;
        }

        private static OrderTimelineStepDto BuildInspectionStep(AppointmentSummaryDto appointment)
        {
            var status = appointment.AppointmentStatus switch
            {
                AppointmentStatus.Completed => OrderTimelineStepStatus.Completed,
                AppointmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                AppointmentStatus.Expired => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.InProgress
            };

            var scheduledStatus = appointment.AppointmentStatus switch
            {
                AppointmentStatus.Proposed => OrderTimelineStepStatus.InProgress,
                AppointmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                AppointmentStatus.Expired => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.Completed
            };

            var startedStatus = appointment.AppointmentStatus switch
            {
                AppointmentStatus.InProgress => OrderTimelineStepStatus.InProgress,
                AppointmentStatus.Completed => OrderTimelineStepStatus.Completed,
                AppointmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                AppointmentStatus.Expired => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.Upcoming
            };

            var completedStatus = appointment.AppointmentStatus switch
            {
                AppointmentStatus.Completed => OrderTimelineStepStatus.Completed,
                AppointmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                AppointmentStatus.Expired => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.Upcoming
            };

            var startedAt = GetInspectionStartedAt(appointment);

            return CreateStep(
                "inspection",
                "Kiểm định sản phẩm",
                BuildAppointmentDescription(appointment),
                status,
                appointment.CompletedAt ?? startedAt ?? appointment.ScheduledAt ?? appointment.CreatedAt,
                new[]
                {
                    CreateStep(
                        "inspection_scheduled",
                        "Xác nhận lịch kiểm định",
                        appointment.ScheduledAt.HasValue
                            ? $"Thời gian dự kiến: {appointment.ScheduledAt.Value:O}"
                            : "Đang chờ hai bên xác nhận lịch kiểm định.",
                        scheduledStatus,
                        appointment.ScheduledAt ?? appointment.CreatedAt),

                    CreateStep(
                        "inspection_started",
                        "Tiến hành kiểm định",
                        "Buyer và seller thực hiện kiểm định theo lịch đã xác nhận.",
                        startedStatus,
                        startedAt),

                    CreateStep(
                        "inspection_completed",
                        "Hoàn tất kiểm định",
                        "Kết quả kiểm định đã được hai bên xử lý.",
                        completedStatus,
                        appointment.CompletedAt)
                });
        }

        private static OrderTimelineStepDto BuildCollectionScheduleStep(AppointmentSummaryDto? appointment)
        {
            if (appointment == null)
            {
                return CreateStep(
                    "collection_schedule",
                    "Đặt lịch giao nhận",
                    "Hai bên chưa tạo lịch giao nhận.",
                    OrderTimelineStepStatus.InProgress);
            }

            var status = appointment.AppointmentStatus switch
            {
                AppointmentStatus.Proposed => OrderTimelineStepStatus.InProgress,
                AppointmentStatus.Scheduled => OrderTimelineStepStatus.Completed,
                AppointmentStatus.InProgress => OrderTimelineStepStatus.Completed,
                AppointmentStatus.Completed => OrderTimelineStepStatus.Completed,
                AppointmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                AppointmentStatus.Expired => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.Upcoming
            };

            return CreateStep(
                "collection_schedule",
                "Lịch giao nhận",
                BuildAppointmentDescription(appointment),
                status,
                appointment.ScheduledAt ?? appointment.CreatedAt);
        }

        private static OrderTimelineStepDto BuildSellerReadyStep(
            OrderDetailDto detail,
            bool prerequisitesCompleted)
        {
            if (detail.Shipment?.SellerReadyAt.HasValue == true)
            {
                return CreateStep(
                    "seller_ready",
                    "Người bán đã chuẩn bị hàng",
                    "Sản phẩm đã sẵn sàng để giao nhận.",
                    OrderTimelineStepStatus.Completed,
                    detail.Shipment.SellerReadyAt);
            }

            var status =
                detail.OrderStatus == OrderStatus.Processing &&
                prerequisitesCompleted
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            var description = detail.DeliveryMethod switch
            {
                DeliveryMethod.GhnDelivery =>
                    "Người bán chuẩn bị sản phẩm để GHN đến lấy hàng.",

                DeliveryMethod.BuyerPickUp =>
                    "Người bán chuẩn bị sản phẩm để người mua đến nhận.",

                DeliveryMethod.SellerDelivers =>
                    "Người bán chuẩn bị sản phẩm trước khi giao cho người mua.",

                _ => "Người bán chuẩn bị sản phẩm để tiến hành giao nhận."
            };

            return CreateStep(
                "seller_ready",
                "Chuẩn bị hàng",
                description,
                status);
        }

        private static OrderTimelineStepDto BuildFulfillmentStep(
            OrderDetailDto detail,
            bool isInspectionCollectNow)
        {
            if (detail.DeliveryMethod == DeliveryMethod.GhnDelivery)
                return BuildGhnFulfillmentStep(detail);

            return BuildDirectFulfillmentStep(
                detail,
                isInspectionCollectNow);
        }

        private static OrderTimelineStepDto BuildGhnFulfillmentStep(OrderDetailDto detail)
        {
            var shipment = detail.Shipment;
            var shipmentStatus = shipment?.ShipmentStatus;

            var pickupCompleted =
                shipment?.PickedUpAt.HasValue == true ||
                shipmentStatus is ShipmentStatus.Delivering
                    or ShipmentStatus.Delivered
                    or ShipmentStatus.Returning
                    or ShipmentStatus.Returned
                    or ShipmentStatus.Damage_Lost;

            var pickupStatus = pickupCompleted
                ? OrderTimelineStepStatus.Completed
                : shipment?.SellerReadyAt.HasValue == true
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            var deliveryStatus = shipmentStatus switch
            {
                ShipmentStatus.Delivered => OrderTimelineStepStatus.Completed,
                ShipmentStatus.Delivering => OrderTimelineStepStatus.InProgress,
                ShipmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                ShipmentStatus.Returning => OrderTimelineStepStatus.Failed,
                ShipmentStatus.Returned => OrderTimelineStepStatus.Failed,
                ShipmentStatus.Damage_Lost => OrderTimelineStepStatus.Failed,
                ShipmentStatus.Exception => OrderTimelineStepStatus.Failed,
                _ => OrderTimelineStepStatus.Upcoming
            };

            var overallStatus = shipmentStatus switch
            {
                ShipmentStatus.Delivered => OrderTimelineStepStatus.Completed,
                ShipmentStatus.Delivering => OrderTimelineStepStatus.InProgress,
                ShipmentStatus.Returning => OrderTimelineStepStatus.InProgress,
                ShipmentStatus.Cancelled => OrderTimelineStepStatus.Cancelled,
                ShipmentStatus.Returned => OrderTimelineStepStatus.Failed,
                ShipmentStatus.Damage_Lost => OrderTimelineStepStatus.Failed,
                ShipmentStatus.Exception => OrderTimelineStepStatus.Failed,
                ShipmentStatus.ReadyToPick when shipment?.SellerReadyAt.HasValue == true =>
                    OrderTimelineStepStatus.InProgress,
                _ => OrderTimelineStepStatus.Upcoming
            };

            var subSteps = new List<OrderTimelineStepDto>
            {
                CreateStep(
                    "ghn_pickup",
                    "GHN lấy hàng",
                    pickupCompleted
                        ? "GHN đã tiếp nhận sản phẩm từ người bán."
                        : "Đang chờ GHN đến lấy hàng.",
                    pickupStatus,
                    shipment?.PickedUpAt),

                CreateStep(
                    "ghn_delivery",
                    "Vận chuyển và giao hàng",
                    BuildGhnStatusDescription(shipmentStatus),
                    deliveryStatus,
                    shipmentStatus == ShipmentStatus.Delivered
                        ? shipment?.DeliveredAt
                        : shipment?.PickedUpAt)
            };

            if (shipmentStatus is ShipmentStatus.Returning or ShipmentStatus.Returned)
            {
                subSteps.Add(CreateStep(
                    "ghn_carrier_return",
                    "GHN chuyển hoàn về người bán",
                    "Đây là chuyển hoàn do quá trình giao hàng GHN không thành công, không phải luồng trả hàng dispute.",
                    shipmentStatus == ShipmentStatus.Returned
                        ? OrderTimelineStepStatus.Completed
                        : OrderTimelineStepStatus.InProgress));
            }

            return CreateStep(
                "shipment",
                "Vận chuyển qua GHN",
                BuildGhnStatusDescription(shipmentStatus),
                overallStatus,
                shipment?.DeliveredAt ?? shipment?.PickedUpAt,
                subSteps);
        }

        private static OrderTimelineStepDto BuildDirectFulfillmentStep(
            OrderDetailDto detail,
            bool isInspectionCollectNow)
        {
            var handoverCompleted =
                detail.SellerHandoverConfirmedAt.HasValue;

            var receivedCompleted =
                detail.BuyerReceivedConfirmedAt.HasValue;

            var handoverStatus = handoverCompleted
                ? OrderTimelineStepStatus.Completed
                : detail.Shipment?.SellerReadyAt.HasValue == true ||
                  isInspectionCollectNow
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            var receivedStatus = receivedCompleted
                ? OrderTimelineStepStatus.Completed
                : handoverCompleted
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            var overallStatus = receivedCompleted
                ? OrderTimelineStepStatus.Completed
                : handoverCompleted ||
                  detail.Shipment?.SellerReadyAt.HasValue == true ||
                  isInspectionCollectNow
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            var title = isInspectionCollectNow
                ? "Giao nhận sau kiểm định"
                : detail.DeliveryMethod switch
                {
                    DeliveryMethod.BuyerPickUp => "Người mua đến nhận hàng",
                    DeliveryMethod.SellerDelivers => "Người bán giao hàng",
                    _ => "Giao nhận sản phẩm"
                };

            return CreateStep(
                "handover",
                title,
                "Hai bên xác nhận lần lượt việc bàn giao và nhận sản phẩm.",
                overallStatus,
                detail.BuyerReceivedConfirmedAt ??
                detail.SellerHandoverConfirmedAt,
                new[]
                {
                    CreateStep(
                        "seller_handover",
                        "Người bán bàn giao hàng",
                        "Người bán xác nhận đã bàn giao sản phẩm.",
                        handoverStatus,
                        detail.SellerHandoverConfirmedAt),

                    CreateStep(
                        "buyer_received",
                        "Người mua nhận hàng",
                        "Người mua xác nhận đã nhận được sản phẩm.",
                        receivedStatus,
                        detail.BuyerReceivedConfirmedAt)
                });
        }

        private static OrderTimelineStepDto BuildCompletionStep(
            OrderDetailDto detail,
            OrderTimelineStepDto? fulfillmentStep)
        {
            if (detail.OrderStatus is OrderStatus.Completed or OrderStatus.Returned ||
                detail.CompletedAt.HasValue)
            {
                return CreateStep(
                    "order_completed",
                    "Hoàn tất đơn hàng",
                    "Người mua đã nhận hàng và đơn hàng đã hoàn thành.",
                    OrderTimelineStepStatus.Completed,
                    detail.CompletedAt);
            }

            var status =
                detail.OrderStatus == OrderStatus.Processing &&
                fulfillmentStep?.Status == OrderTimelineStepStatus.Completed
                    ? OrderTimelineStepStatus.InProgress
                    : OrderTimelineStepStatus.Upcoming;

            return CreateStep(
                "order_completed",
                "Hoàn tất đơn hàng",
                status == OrderTimelineStepStatus.InProgress
                    ? "Hàng đã được giao. Đang chờ người mua xác nhận nhận hàng."
                    : "Đơn hàng sẽ hoàn tất sau khi người mua xác nhận nhận hàng.",
                status);
        }

        private static OrderTimelineStepDto BuildDisputeStep(OrderDetailDto detail)
        {
            var status = detail.Dispute.LatestDisputeStatus switch
            {
                DisputeStatus.Pending => OrderTimelineStepStatus.InProgress,
                DisputeStatus.UnderReview => OrderTimelineStepStatus.InProgress,
                DisputeStatus.AwaitingReturn => OrderTimelineStepStatus.InProgress,
                DisputeStatus.Closed => OrderTimelineStepStatus.Cancelled,
                _ => OrderTimelineStepStatus.Completed
            };

            var description = detail.Dispute.LatestDisputeStatus switch
            {
                DisputeStatus.Pending => "Tranh chấp đã được gửi và đang chờ moderator tiếp nhận.",
                DisputeStatus.UnderReview => "Moderator đang xem xét tranh chấp.",
                DisputeStatus.AwaitingReturn => "Moderator đã đưa ra kết luận và đang chờ xác nhận trả hàng.",
                DisputeStatus.Resolved => "Tranh chấp đã được giải quyết.",
                DisputeStatus.Rejected => "Tranh chấp đã bị từ chối.",
                DisputeStatus.Closed => "Tranh chấp đã được đóng.",
                _ => "Đang xử lý tranh chấp."
            };

            return CreateStep(
                "dispute",
                "Xử lý tranh chấp",
                description,
                status,
                detail.Dispute.LatestDisputeResolvedAt ??
                detail.Dispute.LatestDisputeCreatedAt);
        }

        private static OrderTimelineStepDto BuildReturnStep(OrderDetailDto detail)
        {
            var returnCompleted =
                detail.OrderStatus == OrderStatus.Returned ||
                detail.ReturnedAt.HasValue;

            var buyerReturnAccepted =
                detail.BuyerReturnConfirmedAt.HasValue ||
                detail.SellerReturnReceivedAt.HasValue ||
                returnCompleted;

            var returnVerified =
                detail.SellerReturnReceivedAt.HasValue ||
                returnCompleted;

            return CreateStep(
                "return_refund",
                "Trả hàng và hoàn tiền",
                returnCompleted
                    ? "Việc trả hàng đã được xác nhận và khoản tiền giữ đã được hoàn lại."
                    : "Đang chờ các bên hoàn tất xác nhận trả hàng.",
                returnCompleted
                    ? OrderTimelineStepStatus.Completed
                    : OrderTimelineStepStatus.InProgress,
                detail.ReturnedAt ??
                detail.SellerReturnReceivedAt ??
                detail.BuyerReturnConfirmedAt,
                new[]
                {
                    CreateStep(
                        "buyer_return",
                        "Người mua trả hàng",
                        detail.ReturnDueAt.HasValue
                            ? $"Hạn xác nhận trả hàng: {detail.ReturnDueAt.Value:O}"
                            : "Người mua xác nhận đã trả sản phẩm.",
                        buyerReturnAccepted
                            ? OrderTimelineStepStatus.Completed
                            : OrderTimelineStepStatus.InProgress,
                        detail.BuyerReturnConfirmedAt ??
                        detail.SellerReturnReceivedAt ??
                        detail.ReturnedAt),

                    CreateStep(
                        "return_verified",
                        "Xác nhận đã nhận hàng trả",
                        "Người bán hoặc moderator xác nhận việc trả hàng.",
                        returnVerified
                            ? OrderTimelineStepStatus.Completed
                            : buyerReturnAccepted
                                ? OrderTimelineStepStatus.InProgress
                                : OrderTimelineStepStatus.Upcoming,
                        detail.SellerReturnReceivedAt ??
                        detail.ReturnedAt),

                    CreateStep(
                        "refund_completed",
                        "Hoàn tiền",
                        "Hoàn toàn bộ khoản tiền còn được giữ của order cho người mua.",
                        returnCompleted
                            ? OrderTimelineStepStatus.Completed
                            : OrderTimelineStepStatus.Upcoming,
                        detail.ReturnedAt)
                });
        }

        private static bool ShouldShowReturnStep(OrderDetailDto detail)
        {
            return detail.Dispute.LatestDisputeStatus == DisputeStatus.AwaitingReturn ||
                   detail.BuyerReturnConfirmedAt.HasValue ||
                   detail.SellerReturnReceivedAt.HasValue ||
                   detail.ReturnedAt.HasValue ||
                   detail.OrderStatus == OrderStatus.Returned;
        }

        private static bool HasFulfillmentProgress(OrderDetailDto detail)
        {
            return detail.SellerHandoverConfirmedAt.HasValue ||
                   detail.BuyerReceivedConfirmedAt.HasValue ||
                   detail.Shipment?.PickedUpAt.HasValue == true ||
                   detail.Shipment?.DeliveredAt.HasValue == true ||
                   detail.Shipment?.ShipmentStatus is ShipmentStatus.Delivering
                       or ShipmentStatus.Delivered
                       or ShipmentStatus.Returning
                       or ShipmentStatus.Returned
                       or ShipmentStatus.Damage_Lost;
        }

        private static DateTime? GetInspectionStartedAt(AppointmentSummaryDto appointment)
        {
            var buyerCheckAt =
                appointment.InspectionCheckIn?.BuyerCheckAt;

            var sellerCheckAt =
                appointment.InspectionCheckIn?.SellerCheckAt;

            if (buyerCheckAt.HasValue && sellerCheckAt.HasValue)
            {
                return buyerCheckAt.Value <= sellerCheckAt.Value
                    ? buyerCheckAt
                    : sellerCheckAt;
            }

            return buyerCheckAt ?? sellerCheckAt;
        }

        private static string BuildAppointmentDescription(AppointmentSummaryDto appointment)
        {
            return appointment.AppointmentStatus switch
            {
                AppointmentStatus.Proposed => "Lịch đang chờ bên còn lại xác nhận.",
                AppointmentStatus.Scheduled => "Lịch đã được hai bên xác nhận.",
                AppointmentStatus.InProgress => "Cuộc hẹn đang được thực hiện.",
                AppointmentStatus.Completed => "Cuộc hẹn đã hoàn tất.",
                AppointmentStatus.Cancelled => "Lịch đã bị hủy hoặc được thay thế.",
                AppointmentStatus.Expired => "Lịch đã quá hạn mà chưa hoàn tất.",
                _ => "Chưa xác định trạng thái lịch."
            };
        }

        private static string BuildGhnStatusDescription(ShipmentStatus? status)
        {
            return status switch
            {
                ShipmentStatus.ReadyToPick => "Đang chờ GHN đến lấy hàng.",
                ShipmentStatus.Delivering => "GHN đã lấy hàng và đang vận chuyển.",
                ShipmentStatus.Delivered => "GHN đã giao hàng thành công.",
                ShipmentStatus.Cancelled => "Vận đơn GHN đã bị hủy.",
                ShipmentStatus.Returning => "GHN đang chuyển hoàn hàng về người bán.",
                ShipmentStatus.Returned => "GHN đã chuyển hoàn hàng về người bán.",
                ShipmentStatus.Damage_Lost => "Sản phẩm bị hư hỏng hoặc thất lạc trong quá trình vận chuyển.",
                ShipmentStatus.Exception => "Vận đơn đang gặp ngoại lệ và cần được kiểm tra.",
                _ => "Vận đơn GHN chưa được khởi tạo."
            };
        }

        private static OrderTimelineStepDto CreateStep(
            string code,
            string title,
            string? description,
            OrderTimelineStepStatus status,
            DateTime? occurredAt = null,
            IReadOnlyList<OrderTimelineStepDto>? subSteps = null)
        {
            return new OrderTimelineStepDto
            {
                Code = code,
                Title = title,
                Description = description,
                Status = status,
                OccurredAt = occurredAt,
                SubSteps = subSteps ?? Array.Empty<OrderTimelineStepDto>()
            };
        }

        private static AppointmentSummaryDto? GetEffectiveAppointment(
            IReadOnlyList<AppointmentSummaryDto> appointments,
            AppointmentType appointmentType)
        {
            var matchingAppointments = appointments
                .Where(x => x.AppointmentType == appointmentType)
                .ToList();

            return matchingAppointments
                .Where(x => x.AppointmentStatus is not AppointmentStatus.Cancelled and not AppointmentStatus.Expired)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault()
                ?? matchingAppointments
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();
        }
    }
}
