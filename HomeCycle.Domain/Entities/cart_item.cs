using System;

namespace HomeCycle.Domain.Entities;

public class cart_item
{
    public Guid CartItemId { get; set; }
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual post? Post { get; set; }
}
