using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public sealed class NegotiationListItemResponse
    {
        public Guid NegotiationId { get; set; }
        public Guid OfferId { get; set; }
        public Guid PostId { get; set; }

        public Guid OtherPartyId { get; set; }
        public string OtherPartyName { get; set; } = string.Empty;
        public string? OtherPartyAvatarUrl { get; set; }

        public decimal? CurrentOfferPrice { get; set; }
        public int CurrentOfferQuantity { get; set; }
        public int? CurrentOfferVersion { get; set; }

        public NegotiationStatus NegotiationStatus { get; set; }

        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public int UnreadCount { get; set; }
    }
}
