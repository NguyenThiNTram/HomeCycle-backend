using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public class ConversationMessagesReadResponse
    {
        public Guid ConversationId { get; set; }
        public Guid ReaderId { get; set; }
        public DateTime ReadAt { get; set; }
        public int UpdatedCount { get; set; }
    }
}
