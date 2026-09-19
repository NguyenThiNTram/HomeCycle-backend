using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public sealed class ModeratorNegotiationListItemDto
    {
        public Guid NegotiationId { get; set; }

        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }

        public Guid DisputeId { get; set; }
        public DisputeStatus? DisputeStatus { get; set; }
        public DateTime DisputeCreatedAt { get; set; }

        public string BuyerUsername { get; set; } = string.Empty;
        public string SellerUsername { get; set; } = string.Empty;

        public NegotiationStatus? NegotiationStatus { get; set; }
        public decimal? FinalPrice { get; set; }
        public int? FinalQuantity { get; set; }

        public DateTime? LastMessageAt { get; set; }
    }

    public sealed class ModeratorNegotiationDetailDto
    {
        public Guid NegotiationId { get; set; }
        public NegotiationStatus? NegotiationStatus { get; set; }
        public decimal? FinalPrice { get; set; }
        public int? FinalQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }

        public Guid DisputeId { get; set; }
        public DisputeStatus? DisputeStatus { get; set; }
        public DateTime DisputeCreatedAt { get; set; }
        public DateTime? DisputeResolvedAt { get; set; }

        public Guid BuyerId { get; set; }
        public string BuyerUsername { get; set; } = string.Empty;

        public Guid SellerId { get; set; }
        public string SellerUsername { get; set; } = string.Empty;
    }

    public sealed class ModeratorNegotiationMessageDto
    {
        public Guid MessageId { get; set; }

        public Guid SenderId { get; set; }
        public string? SenderUsername { get; set; }

        public MessageType? MessageType { get; set; }
        public string? MessageContent { get; set; }

        public decimal? OfferPrice { get; set; }
        public int? OfferQuantity { get; set; }
        public MessageOfferStatus? OfferStatus { get; set; }

        public decimal? BasePriceSnapshot { get; set; }
        public string? MediaUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
