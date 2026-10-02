using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Entities
{
    public class dispute_response
    {
        public Guid DisputeResponseId { get; set; }
        public Guid DisputeId { get; set; }
        public Guid ResponderId { get; set; }
        public int ResponseType { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
