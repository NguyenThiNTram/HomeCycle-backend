using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderDetailDto
    {
        public order Order { get; set; } = null!;   // đã có OrderCode, ProductName, Quantity, giá, trạng thái, CreatedAt
        public string? ThumbnailUrl { get; set; }
        public string? PostDescription { get; set; }
        public string? CounterpartyName { get; set; }
        public Guid NegotiationId { get; set; }

        public int? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }

        public ReviewSummaryDto Review { get; set; } = new();
        public ShipmentSummaryDto? Shipment { get; set; }
        public DisputeSummaryDto Dispute { get; set; } = new();
    }

    public class ReviewSummaryDto
    {
        public Guid? ReviewId { get; set; }
        public bool HasReviewed { get; set; }
        public bool CanReview { get; set; }
        public int? Rating { get; set; }
    }

    public class ShipmentSummaryDto
    {
        public Guid? ShipmentId { get; set; }
        public int? ShipmentStatus { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class DisputeSummaryDto
    {
        public bool HasActiveDispute { get; set; }
        public Guid? LatestDisputeId { get; set; }
    }
}
