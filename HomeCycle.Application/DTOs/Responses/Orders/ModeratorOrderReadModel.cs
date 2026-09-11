using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public sealed class ModeratorOrderReadModel
    {
        public Guid OrderId { get; init; }
        public Guid AgreementId { get; init; }
        public Guid PostId { get; init; }

        public string OrderCode { get; init; } = string.Empty;
        public string? ProductName { get; init; }
        public string? ThumbnailUrl { get; init; }

        public int Quantity { get; init; }

        public decimal? FinalTotalAmount { get; init; }
        public decimal? AmountPaid { get; init; }
        public decimal? AmountRemaining { get; init; }

        public int? OrderStatus { get; init; }
        public int? PaymentStatus { get; init; }

        public Guid BuyerId { get; init; }
        public string? BuyerUsername { get; init; }
        public string? BuyerPhoneNumber { get; init; }
        public string? BuyerAvatarUrl { get; init; }

        public Guid SellerId { get; init; }
        public string? SellerUsername { get; init; }
        public string? SellerPhoneNumber { get; init; }
        public string? SellerAvatarUrl { get; init; }

        public bool HasActiveDispute { get; init; }

        public Guid? LatestDisputeId { get; init; }
        public int? LatestDisputeStatus { get; init; }

        public bool HasInspection { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
