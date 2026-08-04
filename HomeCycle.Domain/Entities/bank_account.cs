using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class bank_account
{
    public Guid UserBankId { get; set; }
    public Guid UserId { get; set; }

    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }

    public VerifyStatus? VerifyStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public bank_account()
    {
    }

    public bank_account(Guid UserBankId, Guid UserId)
    {
        this.UserBankId = UserBankId;
        this.UserId = UserId;
    }
}
