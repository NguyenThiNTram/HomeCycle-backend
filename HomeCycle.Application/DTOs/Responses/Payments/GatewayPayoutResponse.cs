using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class GatewayPayoutResponse
    {
        public string PayoutId { get; set; } = string.Empty;
        public string ApprovalState { get; set; } = string.Empty;
    }
}
