using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Index("AccountNumber", Name = "uq_bank_account", IsUnique = true)]
public partial class Bank_Account
{
    [Key]
    public Guid UserBankId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(20)]
    public string? BankCode { get; set; }

    [StringLength(255)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? AccountNumber { get; set; }

    [StringLength(255)]
    public string? AccountName { get; set; }

    public int? VerifyStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Bank_Accounts")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("UserBank")]
    public virtual ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
}
