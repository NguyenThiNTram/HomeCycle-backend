using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public sealed class ConversationUpdatedResponse
    {
        public Guid ConversationId { get; set; }
        public Guid? NegotiationId { get; set; }
        public Guid? LastSenderId { get; set; }
        public string? LastMessagePreview { get; set; }
        public MessageType? LastMessageType { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public decimal? CurrentOfferPrice { get; set; }
        public int CurrentOfferQuantity { get; set; }
        public int? CurrentOfferVersion { get; set; }
        public NegotiationStatus? NegotiationStatus { get; set; }
        public Dictionary<Guid, int> UnreadCountByUser { get; set; } = new();
    }
}
