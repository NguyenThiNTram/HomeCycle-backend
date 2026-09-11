using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class ModeratorOrderPartyDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class ModeratorOrderListItemDto
    {
        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? ThumbnailUrl { get; set; }

        public int Quantity { get; set; }

        public decimal? FinalTotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountRemaining { get; set; }

        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public ModeratorOrderPartyDto Buyer { get; set; } = new();
        public ModeratorOrderPartyDto Seller { get; set; } = new();

        public bool HasActiveDispute { get; set; }
        public Guid? LatestDisputeId { get; set; }
        public DisputeStatus? LatestDisputeStatus { get; set; }

        public bool HasInspection { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ModeratorOrderDetailDto
    {
        public Guid OrderId { get; set; }
        public Guid AgreementId { get; set; }
        public Guid PostId { get; set; }

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

        public DateTime? BuyerReturnConfirmedAt { get; set; }
        public DateTime? SellerReturnReceivedAt { get; set; }
        public DateTime? ReturnDueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public DateTime? DisputeWindowEndsAt { get; set; }

        public string? ThumbnailUrl { get; set; }
        public string? PostDescription { get; set; }

        public OrderCancellationDto? Cancellation { get; set; }

        public ModeratorOrderPartyDto Buyer { get; set; } = new();
        public ModeratorOrderPartyDto Seller { get; set; } = new();

        public PaymentSummaryDto? Payment { get; set; }
        public ShipmentSummaryDto? Shipment { get; set; }

        public IReadOnlyList<AppointmentSummaryDto> Appointments { get; set; }
            = Array.Empty<AppointmentSummaryDto>();

        public DisputeSummaryDto Dispute { get; set; } = new();

        public IReadOnlyList<OrderTimelineStepDto> Timeline { get; set; }
            = Array.Empty<OrderTimelineStepDto>();
    }
}
