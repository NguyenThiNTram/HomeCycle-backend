using HomeCycle.Domain.Enums;
using MathNet.Numerics.RootFinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletLedgerResponseDto
    {
        public Guid LedgerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public LedgerDirection Direction { get; set; }
        public BalanceType BalanceType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;

        // Context từ Wallet_Transaction
        public TransactionType? TransactionType { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
