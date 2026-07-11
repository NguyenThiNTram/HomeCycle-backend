using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public partial class wallet_transaction
{
    public Guid WalletTransactionId { get; set; }
    public Guid? FromWalletId { get; set; }
    public Guid? ToWalletId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ReferenceId { get; set; }

    public int? ReferenceType { get; set; }
    public int? TransactionType { get; set; }
    public decimal? Amount { get; set; }

    public int? WalletTransactionStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public wallet_transaction()
    {
    }

    public wallet_transaction(Guid WalletTransactionId)
    {
        this.WalletTransactionId = WalletTransactionId;
    }
}
