using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class UserWalletBalanceImpactDto
    {
        public Guid LedgerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public LedgerDirection Direction { get; set; }
        public BalanceType BalanceType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UserWalletTransactionListItemDto
    {
        public Guid WalletTransactionId { get; set; }

        public Guid? PaymentId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public TransactionType? TransactionType { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? ReferenceCode { get; set; }

        public decimal Amount { get; set; }
        public WalletTransactionStatus? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Description { get; set; } = string.Empty;

        public List<UserWalletBalanceImpactDto> BalanceImpacts { get; set; } = new();
    }
}
