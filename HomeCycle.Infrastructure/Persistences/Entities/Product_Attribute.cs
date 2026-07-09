using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Product_Attribute")]
[Index("ProductTypeId", Name = "idx_product_attribute_type")]
public partial class Product_Attribute
{
    [Key]
    public Guid AttributeId { get; set; }

    public Guid ProductTypeId { get; set; }

    [StringLength(255)]
    public string? AttributeName { get; set; }

    public int? DataType { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsFilterable { get; set; }

    public bool IsRequired { get; set; }

    [ForeignKey("ProductTypeId")]
    [InverseProperty("Product_Attributes")]
    public virtual Product_Type ProductType { get; set; } = null!;

    [InverseProperty("Attribute")]
    public virtual ICollection<Product_Attribute_Option> Product_Attribute_Options { get; set; } = new List<Product_Attribute_Option>();

    [InverseProperty("Attribute")]
    public virtual ICollection<Product_Attribute_Value> Product_Attribute_Values { get; set; } = new List<Product_Attribute_Value>();
}
