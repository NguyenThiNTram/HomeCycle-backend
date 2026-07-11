using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Product_Type")]
[Index("CategoryId", "ProductTypeName", Name = "ux_product_type_name", IsUnique = true)]
public partial class Product_Type
{
    [Key]
    public Guid ProductTypeId { get; set; }

    public Guid CategoryId { get; set; }

    [StringLength(255)]
    public string? ProductTypeName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("ProductType")]
    public virtual ICollection<Business_Product_Type> Business_Product_Types { get; set; } = new List<Business_Product_Type>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Product_Types")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("ProductType")]
    public virtual ICollection<Product_Attribute> Product_Attributes { get; set; } = new List<Product_Attribute>();

    [InverseProperty("ProductType")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
