using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class OrderFinancialEventDto
    {
        public Guid WalletTransactionId { get; set; }
        public Guid? PaymentId { get; set; }
        public TransactionType? TransactionType { get; set; }
        public WalletTransactionStatus? Status { get; set; }
        public decimal Amount { get; set; }
        public Guid? FromWalletId { get; set; }
        public Guid? ToWalletId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
