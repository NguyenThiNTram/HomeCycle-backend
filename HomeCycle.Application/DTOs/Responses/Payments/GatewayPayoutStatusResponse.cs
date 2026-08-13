using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class GatewayPayoutStatusResponse
    {
        public string PayoutId { get; set; } = string.Empty;
        public string ApprovalState { get; set; } = string.Empty;      // tầng payOS có duyệt/xử lý không
        public string? TransactionState { get; set; }                  // tầng tiền có thật sự chuyển không
        public string? FailureReason { get; set; }
    }
}
