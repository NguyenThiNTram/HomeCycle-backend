using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Product_Attribute_Option")]
[Index("AttributeId", Name = "idx_product_attribute_option_attribute")]
public partial class Product_Attribute_Option
{
    [Key]
    public Guid OptionId { get; set; }

    public Guid AttributeId { get; set; }

    [StringLength(255)]
    public string? OptionValue { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsDefault { get; set; }

    [ForeignKey("AttributeId")]
    [InverseProperty("Product_Attribute_Options")]
    public virtual Product_Attribute Attribute { get; set; } = null!;

    [InverseProperty("Option")]
    public virtual ICollection<Product_Attribute_Value> Product_Attribute_Values { get; set; } = new List<Product_Attribute_Value>();
}
