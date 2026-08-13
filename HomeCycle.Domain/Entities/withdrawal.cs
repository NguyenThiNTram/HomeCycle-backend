using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class withdrawal
{
    public Guid WithdrawalId { get; set; }
    public Guid WalletId { get; set; }
    public Guid UserBankId { get; set; }

    public decimal? Amount { get; set; }

    public int? WithdrawalStatus { get; set; }

    public DateTime? RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedBy { get; set; }
    public string? RejectReason { get; set; }

    public withdrawal()
    {
    }

    public withdrawal(Guid WithdrawalId, Guid WalletId, Guid UserBankId)
    {
        this.WithdrawalId = WithdrawalId;
        this.WalletId = WalletId;
        this.UserBankId = UserBankId;
    }
}
