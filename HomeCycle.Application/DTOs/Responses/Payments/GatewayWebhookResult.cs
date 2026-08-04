using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class GatewayWebhookResult
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Status { get; set; }
        public string ReferenceTransactionId { get; set; } 
        public string Description { get; set; }
    }
}
