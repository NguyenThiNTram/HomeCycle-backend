using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public partial class wallet_ledger
{
    public Guid LedgerId { get; set; }
    public Guid WalletTransactionId { get; set; }
    public Guid WalletId { get; set; }

    public int? Direction { get; set; }
    public decimal? Amount { get; set; }
    public decimal? BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }

    public wallet_ledger()
    {
    }

    public wallet_ledger(Guid LedgerId, Guid WalletTransactionId, Guid WalletId)
    {
        this.LedgerId = LedgerId;
        this.WalletTransactionId = WalletTransactionId;
        this.WalletId = WalletId;
    }
}
