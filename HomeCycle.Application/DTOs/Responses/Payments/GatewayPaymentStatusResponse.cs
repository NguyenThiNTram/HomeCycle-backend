using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class GatewayPaymentStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public long? OrderCode { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? TransactionId { get; set; }
    }
}
