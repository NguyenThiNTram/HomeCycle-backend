using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Infrastructure;

[Table("Cart_Item")]
public partial class Cart_Item
{
    [Key]
    public Guid CartItemId { get; set; }

    public Guid UserId { get; set; }

    public Guid PostId { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Cart_Items")]
    public virtual Post? Post { get; set; }

    [InverseProperty("Cart_Items")]
    public virtual User? User { get; set; }
}
