using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Conversations
{
    public class ConversationParticipantResponse
    {
        public Guid UserId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }
}
