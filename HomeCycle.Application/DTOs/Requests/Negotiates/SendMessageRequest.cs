using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Negotiates
{
    public sealed class SendMessageRequest
    {
        public string MessageContent { get; set; } = null!;

        // FE tạo UUID trước mỗi lần gửi.
        public Guid ClientMessageId { get; set; }
    }
}
