using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Product")]
[Index("PostId", Name = "Product_PostId_key", IsUnique = true)]
public partial class Product
{
    [Key]
    public Guid ProductId { get; set; }

    public Guid PostId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid ProductTypeId { get; set; }

    [StringLength(255)]
    public string? ProductName { get; set; }

    [StringLength(255)]
    public string? BrandName { get; set; }

    [StringLength(100)]
    public string? SpaceUsage { get; set; }

    [StringLength(100)]
    public string? ModelNumber { get; set; }

    [Precision(18, 2)]
    public decimal? OriginalPrice { get; set; }

    [Precision(10, 2)]
    public decimal? Length { get; set; }

    [Precision(10, 2)]
    public decimal? Width { get; set; }

    [Precision(10, 2)]
    public decimal? Height { get; set; }

    [Precision(10, 2)]
    public decimal? Weight { get; set; }

    public int? FunctionalityStatus { get; set; }

    public int? UsageDuration { get; set; }

    public int? DamageLevel { get; set; }

    public string? DetailDescription { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category Category { get; set; } = null!;

    [ForeignKey("PostId")]
    [InverseProperty("Product")]
    public virtual Post Post { get; set; } = null!;

    [ForeignKey("ProductTypeId")]
    [InverseProperty("Products")]
    public virtual Product_Type ProductType { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<Product_Attribute_Value> Product_Attribute_Values { get; set; } = new List<Product_Attribute_Value>();
}
