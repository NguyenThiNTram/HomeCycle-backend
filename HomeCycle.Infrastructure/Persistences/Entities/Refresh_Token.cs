using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Refresh_Token")]
[Index("UserId", Name = "idx_refresh_token_userid")]
public partial class Refresh_Token
{
    [Key]
    public Guid RefreshTokenId { get; set; }

    public Guid UserId { get; set; }

    public string? Token { get; set; }

    public string? ReplacedByToken { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Refresh_Tokens")]
    public virtual User User { get; set; } = null!;
}
