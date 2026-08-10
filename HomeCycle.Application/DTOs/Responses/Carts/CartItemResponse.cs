using HomeCycle.Application.DTOs.Responses.Posts;
using System;
using System.Collections.Generic;

namespace HomeCycle.Application.DTOs.Responses.Carts
{
    public class CartItemResponse
    {
        public Guid CartItemId { get; set; }
        public Guid PostId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }

        public PostResponse Post { get; set; } = null!;
    }

    public class CartResponse
    {
        public IReadOnlyList<CartItemResponse> Items { get; set; } = Array.Empty<CartItemResponse>();

        public int TotalQuantity { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
