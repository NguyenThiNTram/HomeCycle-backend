using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;

namespace HomeCycle.Application.DTOs.Responses.Conversations
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

        // tin chưa đọc tổng của Conversation
        public Dictionary<Guid, int> ConversationUnreadByUser { get; set; }

        // tin chưa đọc chi tiết từng Negotiation
        public Dictionary<Guid, Dictionary<Guid, int?>> NegotiationUnreadByUser { get; set; }
    }
}
