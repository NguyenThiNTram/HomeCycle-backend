using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Messages
{
    public class UnreadCountResult
    {
        public int TotalConversationUnread { get; set; }
        public Dictionary<Guid, int> UnreadByNegotiation { get; set; } = new();
    }
}
