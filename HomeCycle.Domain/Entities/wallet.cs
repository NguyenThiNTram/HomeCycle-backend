using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class wallet
{
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }

    public string? WalletType { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal HoldBalance { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public wallet()
    {
    }

    public wallet(Guid WalletId, Guid UserId)
    {
        this.WalletId = WalletId;
        this.UserId = UserId;
    }
}
