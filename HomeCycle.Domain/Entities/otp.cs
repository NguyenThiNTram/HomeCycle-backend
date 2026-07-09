using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class otp
{
    public Guid OtpId { get; set; }
    public Guid UserId { get; set; }

    public string? Code { get; set; }
    public string? Purpose { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? ExpiredAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public otp()
    {
    }

    public otp(Guid OtpId, Guid UserId)
    {
        this.OtpId = OtpId;
        this.UserId = UserId;
    }
}
