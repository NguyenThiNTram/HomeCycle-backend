using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Business_Product_Type")]
[Index("BusinessProfileId", Name = "idx_business_product_type_business")]
[Index("ProductTypeId", Name = "idx_business_product_type_product")]
[Index("BusinessProfileId", "ProductTypeId", Name = "uq_bpt", IsUnique = true)]
[Index("BusinessProfileId", "ProductTypeId", Name = "ux_business_product_type", IsUnique = true)]
public partial class Business_Product_Type
{
    [Key]
    public Guid BusinessProductTypeId { get; set; }

    public Guid BusinessProfileId { get; set; }

    public Guid ProductTypeId { get; set; }

    public int? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("BusinessProfileId")]
    [InverseProperty("Business_Product_Types")]
    public virtual Business_Profile BusinessProfile { get; set; } = null!;

    [ForeignKey("ProductTypeId")]
    [InverseProperty("Business_Product_Types")]
    public virtual Product_Type ProductType { get; set; } = null!;
}
