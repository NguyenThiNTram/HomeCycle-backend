using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class refresh_token
{
    public Guid RefreshTokenId { get; set; }
    public Guid UserId { get; set; }

    public string? Token { get; set; }
    public string? ReplacedByToken { get; set; }

    public DateTime? RevokedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public refresh_token()
    {
    }

    public refresh_token(Guid RefreshTokenId, Guid UserId)
    {
        this.RefreshTokenId = RefreshTokenId;
        this.UserId = UserId;
    }
}
