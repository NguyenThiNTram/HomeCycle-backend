using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Wallet")]
[Index("UserId", "WalletType", Name = "uq_wallet", IsUnique = true)]
[Index("UserId", "WalletType", Name = "ux_wallet_user_type", IsUnique = true)]
public partial class Wallet
{
    [Key]
    public Guid WalletId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string? WalletType { get; set; }

    [Precision(18, 2)]
    public decimal AvailableBalance { get; set; }

    [Precision(18, 2)]
    public decimal HoldBalance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Wallets")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Wallet")]
    public virtual ICollection<Wallet_Ledger> Wallet_Ledgers { get; set; } = new List<Wallet_Ledger>();

    [InverseProperty("FromWallet")]
    public virtual ICollection<Wallet_Transaction> Wallet_TransactionFromWallets { get; set; } = new List<Wallet_Transaction>();

    [InverseProperty("ToWallet")]
    public virtual ICollection<Wallet_Transaction> Wallet_TransactionToWallets { get; set; } = new List<Wallet_Transaction>();

    [InverseProperty("Wallet")]
    public virtual ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
}
