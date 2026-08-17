using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderConfirmationResponseDto
    {
        public Guid OrderId { get; set; }
        public int? OrderStatus { get; set; }
        public DateTime? SellerHandoverConfirmedAt { get; set; }
        public DateTime? BuyerReceivedConfirmedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public OrderCompletionSource? CompletionSource { get; set; }
    }
}
