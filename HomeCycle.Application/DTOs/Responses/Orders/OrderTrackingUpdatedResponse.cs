using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public sealed class OrderTrackingUpdatedResponse
    {
        public Guid OrderId { get; init; }

        // Thời điểm server đọc snapshot sau khi commit (UTC). Không dùng Order.UpdatedAt vì vận chuyển,
        // lịch hẹn hay khiếu nại có thể đổi mà dòng Order không đổi.
        // Client bỏ qua event có UpdatedAt cũ hơn dữ liệu đang hiển thị.
        public DateTime UpdatedAt { get; init; }

        // Trạng thái tối thiểu để cập nhật nhanh màn đơn hàng. Không có timeline và các cờ Can* vì
        // timeline cần cả chi tiết đơn, còn Can* phụ thuộc người xem: client vẫn gọi lại GET /orders/{id}.
        // Null khi server không đọc được snapshot: client chỉ coi event là tín hiệu tải lại.
        public OrderRealtimeSnapshotDto? Order { get; init; }
    }

    // Enum gửi dạng chuỗi giống REST (SignalR mặc định gửi enum dạng số).
    public sealed class OrderRealtimeSnapshotDto
    {
        public string? OrderStatus { get; init; }
        public string? PaymentStatus { get; init; }
        public string? DeliveryMethod { get; init; }

        public decimal? FinalTotalAmount { get; init; }
        public decimal? AmountPaid { get; init; }
        public decimal? AmountRemaining { get; init; }

        public DateTime? SellerHandoverConfirmedAt { get; init; }
        public DateTime? BuyerReceivedConfirmedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public string? CompletionSource { get; init; }
        public DateTime? DisputeWindowEndsAt { get; init; }

        public DateTime? ReturnDueAt { get; init; }
        public DateTime? BuyerReturnConfirmedAt { get; init; }
        public DateTime? SellerReturnReceivedAt { get; init; }
        public DateTime? ReturnedAt { get; init; }

        public OrderCancellationDto? Cancellation { get; init; }
        public OrderRealtimeShipmentDto? Shipment { get; init; }
        public OrderRealtimeDisputeDto Dispute { get; init; } = new();

        // Order.UpdatedAt trong database, giữ để đối chiếu với REST.
        public DateTime UpdatedAt { get; init; }
    }

    public sealed class OrderRealtimeShipmentDto
    {
        public Guid ShipmentId { get; init; }
        public string? ShipmentStatus { get; init; }
        public DateTime? SellerReadyAt { get; init; }
        public DateTime? PickedUpAt { get; init; }
        public DateTime? DeliveredAt { get; init; }

        // Chỉ có với đơn GHN.
        public string? TrackingCode { get; init; }
        public string? CarrierStatus { get; init; }
        public string? CreationStatus { get; init; }
        public DateTime? ExpectedDeliveryAt { get; init; }
    }

    public sealed class OrderRealtimeDisputeDto
    {
        public bool HasActiveDispute { get; init; }
        public Guid? LatestDisputeId { get; init; }
        public string? LatestDisputeStatus { get; init; }
        public DateTime? LatestDisputeCreatedAt { get; init; }
        public DateTime? LatestDisputeResolvedAt { get; init; }
    }
}
