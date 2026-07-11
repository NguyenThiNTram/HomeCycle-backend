using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("OTP")]
[Index("UserId", Name = "idx_otp_userid")]
public partial class OTP
{
    [Key]
    public Guid OtpId { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(20)]
    public string? Code { get; set; }

    public string? Email { get; set; }

    [StringLength(50)]
    public string? Purpose { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("OTPs")]
    public virtual User User { get; set; } = null!;
}
