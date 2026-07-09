using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Withdrawal")]
public partial class Withdrawal
{
    [Key]
    public Guid WithdrawalId { get; set; }

    public Guid WalletId { get; set; }

    public Guid UserBankId { get; set; }

    [Precision(18, 2)]
    public decimal? Amount { get; set; }

    public int? WithdrawalStatus { get; set; }

    public DateTime? RequestedAt { get; set; }

    [ForeignKey("UserBankId")]
    [InverseProperty("Withdrawals")]
    public virtual Bank_Account UserBank { get; set; } = null!;

    [ForeignKey("WalletId")]
    [InverseProperty("Withdrawals")]
    public virtual Wallet Wallet { get; set; } = null!;
}
