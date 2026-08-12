using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderListItemDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int Quantity { get; set; }
        public decimal? FinalTotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountRemaining { get; set; }
        public int? OrderStatus { get; set; }
        public int? PaymentStatus { get; set; }
    }

}
