using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderDetailDto
    {
        public Guid OrderId { get; set; }
        public Guid AgreementId { get; set; }
        public Guid PostId { get; set; }
        public Guid NegotiationId { get; set; }

        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int Quantity { get; set; }

        public decimal? OriginalTotalAmount { get; set; }
        public decimal? FinalTotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountRemaining { get; set; }
        public decimal? ShippingFee { get; set; }

        public PaymentStatus? PaymentStatus { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public DeliveryMethod? DeliveryMethod { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime? SellerHandoverConfirmedAt { get; set; }
        public DateTime? BuyerReceivedConfirmedAt { get; set; }
        public OrderCompletionSource? CompletionSource { get; set; }

        public string? ThumbnailUrl { get; set; }
        public string? PostDescription { get; set; }

        public DateTime? DisputeWindowEndsAt { get; set; }

        public OrderCancellationDto? Cancellation { get; set; }

        public CounterpartySummaryDto Counterparty { get; set; } = new();

        public PaymentSummaryDto? Payment { get; set; }
        public ShipmentSummaryDto? Shipment { get; set; }

        public IReadOnlyList<AppointmentSummaryDto> Appointments { get; set; } = new List<AppointmentSummaryDto>();

        public IReadOnlyList<review> Reviews { get; set; } = new List<review>();
        public ReviewSummaryDto Review { get; set; } = new();

        public DisputeSummaryDto Dispute { get; set; } = new();
        public OrderActionDto Actions { get; set; } = new();
    }

    public class OrderCancellationDto
    {
        public DateTime CancelledAt { get; set; }
        public Guid? CancelledByUserId { get; set; }
        public string? Reason { get; set; }
    }
    public class CounterpartySummaryDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class PaymentSummaryDto
    {
        public Guid PaymentId { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public decimal? Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class ShipmentSummaryDto
    {
        public Guid ShipmentId { get; set; }
        public ShipmentStatus? ShipmentStatus { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class ReviewSummaryDto
    {
        public Guid? ReviewId { get; set; }
        public bool HasReviewed { get; set; }
        public int? Rating { get; set; }
    }

    public class DisputeSummaryDto
    {
        public bool HasActiveDispute { get; set; }
        public Guid? LatestDisputeId { get; set; }
        public DisputeStatus? LatestDisputeStatus { get; set; }
    }
}
