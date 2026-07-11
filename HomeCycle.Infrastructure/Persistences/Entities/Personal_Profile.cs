using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Personal_Profile")]
[Index("UserId", Name = "Personal_Profile_UserId_key", IsUnique = true)]
[Index("UserId", Name = "ux_personal_profile_userid", IsUnique = true)]
public partial class Personal_Profile
{
    [Key]
    public Guid PersonalProfileId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string? FullName { get; set; }

    [StringLength(50)]
    public string? RepresentativeCode { get; set; }

    [StringLength(255)]
    public string? RepresentativeName { get; set; }

    public DateOnly? RepresentativeDob { get; set; }

    public string? RepresentativeAddress { get; set; }

    [StringLength(500)]
    public string? FrontIDCardImage { get; set; }

    [StringLength(500)]
    public string? BackIDCardImage { get; set; }

    public int? VerificationStatus { get; set; }

    public Guid? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int ReputationScore { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Personal_ProfileUser")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("VerifiedBy")]
    [InverseProperty("Personal_ProfileVerifiedByNavigations")]
    public virtual User? VerifiedByNavigation { get; set; }
}
