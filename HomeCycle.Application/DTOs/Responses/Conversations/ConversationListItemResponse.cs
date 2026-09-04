using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Conversations
{
    public class ConversationListItemResponse
    {
        public Guid ConversationId { get; set; }
        public ConversationParticipantResponse OtherParticipant { get; set; } = new();

        //public Guid OtherUserId { get; set; }
        //public string? OtherUserName { get; set; }
        //public string? OtherUserAvatarUrl { get; set; }

        public Guid? LatestNegotiationId { get; set; }
        public Guid? LatestMessageId { get; set; }
        public Guid? LatestMessageSenderId { get; set; }
        
        public MessageType? LatestMessageType { get; set; }
        public string? LatestMessagePreview { get; set; }
        public DateTime? LatestMessageAt { get; set; }

        public int UnreadCount { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
