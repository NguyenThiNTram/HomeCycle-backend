using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Business_Profile")]
[Index("UserId", Name = "Business_Profile_UserId_key", IsUnique = true)]
[Index("UserId", Name = "ux_business_profile_userid", IsUnique = true)]
public partial class Business_Profile
{
    [Key]
    public Guid BusinessProfileId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string? BusinessName { get; set; }

    public string? BusinessDescription { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(255)]
    public string? FullName { get; set; }

    [StringLength(20)]
    public string IdentityNumber { get; set; }

    public string? BusinessAddress { get; set; }

    [StringLength(100)]
    public string? Ward { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(255)]
    public string? OperatingScope { get; set; }

    [StringLength(255)]
    public int BusinessModel { get; set; }

    public int Status { get; set; }

    public Guid? CurrentModeratorId { get; set; }

    public int ReputationScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("BusinessProfile")]
    public virtual ICollection<Business_Document> Business_Documents { get; set; } = new List<Business_Document>();

    [InverseProperty("BusinessProfile")]
    public virtual ICollection<Business_Product_Type> Business_Product_Types { get; set; } = new List<Business_Product_Type>();

    [InverseProperty("BusinessProfile")]
    public virtual ICollection<Business_Service_Area> Business_Service_Areas { get; set; } = new List<Business_Service_Area>();

    [ForeignKey("UserId")]
    [InverseProperty("Business_Profile")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CurrentModeratorId")]
    public virtual User? CurrentModeratorNavigation { get; set; }
}
