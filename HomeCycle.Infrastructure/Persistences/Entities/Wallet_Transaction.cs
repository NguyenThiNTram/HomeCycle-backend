using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Wallet_Transaction")]
[Index("FromWalletId", Name = "idx_wallet_transaction_from")]
[Index("PaymentId", Name = "idx_wallet_transaction_payment")]
[Index("ReferenceType", "ReferenceId", Name = "idx_wallet_transaction_reference")]
[Index("ToWalletId", Name = "idx_wallet_transaction_to")]
public partial class Wallet_Transaction
{
    [Key]
    public Guid WalletTransactionId { get; set; }

    public Guid? FromWalletId { get; set; }

    public Guid? ToWalletId { get; set; }

    public Guid? PaymentId { get; set; }

    public Guid? ReferenceId { get; set; }

    public int? ReferenceType { get; set; }

    public int? TransactionType { get; set; }

    [Precision(18, 2)]
    public decimal? Amount { get; set; }

    public int? WalletTransactionStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("FromWalletId")]
    [InverseProperty("Wallet_TransactionFromWallets")]
    public virtual Wallet? FromWallet { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("Wallet_Transactions")]
    public virtual Payment? Payment { get; set; }

    [ForeignKey("ToWalletId")]
    [InverseProperty("Wallet_TransactionToWallets")]
    public virtual Wallet? ToWallet { get; set; }

    [InverseProperty("WalletTransaction")]
    public virtual ICollection<Wallet_Ledger> Wallet_Ledgers { get; set; } = new List<Wallet_Ledger>();
}
