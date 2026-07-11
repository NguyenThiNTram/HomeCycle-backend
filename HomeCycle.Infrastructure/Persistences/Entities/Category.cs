using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Category")]
[Index("CategoryName", Name = "uq_category_name", IsUnique = true)]
[Index("CategoryName", Name = "ux_category_name", IsUnique = true)]
public partial class Category
{
    [Key]
    public Guid CategoryId { get; set; }

    [StringLength(255)]
    public string? CategoryName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Product_Type> Product_Types { get; set; } = new List<Product_Type>();

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
