using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Payments
{
    public class GatewayPayoutRequest
    {
        public string ReferenceId { get; set; } = string.Empty; // = WithdrawalId, idempotency key
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ToBin { get; set; } = string.Empty;       // = bank_account.BankCode
        public string ToAccountNumber { get; set; } = string.Empty;
    }
}
