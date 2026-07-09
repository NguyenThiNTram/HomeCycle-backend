using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Business_Service_Area")]
[Index("BusinessProfileId", Name = "idx_business_service_area_business")]
[Index("BusinessProfileId", "City", "District", "Ward", Name = "uq_bsa", IsUnique = true)]
[Index("BusinessProfileId", "City", "District", "Ward", Name = "ux_business_service_area", IsUnique = true)]
public partial class Business_Service_Area
{
    [Key]
    public Guid BusinessServiceAreaId { get; set; }

    public Guid BusinessProfileId { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(100)]
    public string? Ward { get; set; }

    public int? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("BusinessProfileId")]
    [InverseProperty("Business_Service_Areas")]
    public virtual Business_Profile BusinessProfile { get; set; } = null!;
}
