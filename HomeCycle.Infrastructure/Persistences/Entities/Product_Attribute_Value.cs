using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[PrimaryKey("ProductId", "AttributeId")]
[Table("Product_Attribute_Value")]
public partial class Product_Attribute_Value
{
    [Key]
    public Guid ProductId { get; set; }

    [Key]
    public Guid AttributeId { get; set; }

    public Guid? OptionId { get; set; }

    public int? InputType { get; set; }

    public bool? ValueBoolean { get; set; }

    public string? ValueText { get; set; }

    [Precision(18, 2)]
    public decimal? ValueNumber { get; set; }

    [ForeignKey("AttributeId")]
    [InverseProperty("Product_Attribute_Values")]
    public virtual Product_Attribute Attribute { get; set; } = null!;

    [ForeignKey("OptionId")]
    [InverseProperty("Product_Attribute_Values")]
    public virtual Product_Attribute_Option? Option { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Product_Attribute_Values")]
    public virtual Product Product { get; set; } = null!;
}
