using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Wallet_Ledger")]
[Index("WalletTransactionId", Name = "idx_wallet_ledger_transaction")]
[Index("WalletId", Name = "idx_wallet_ledger_wallet")]
public partial class Wallet_Ledger
{
    [Key]
    public Guid LedgerId { get; set; }

    public Guid WalletTransactionId { get; set; }

    public Guid WalletId { get; set; }

    public int? Direction { get; set; }

    [Precision(18, 2)]
    public decimal? Amount { get; set; }

    [Precision(18, 2)]
    public decimal? BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("WalletId")]
    [InverseProperty("Wallet_Ledgers")]
    public virtual Wallet Wallet { get; set; } = null!;

    [ForeignKey("WalletTransactionId")]
    [InverseProperty("Wallet_Ledgers")]
    public virtual Wallet_Transaction WalletTransaction { get; set; } = null!;
}
