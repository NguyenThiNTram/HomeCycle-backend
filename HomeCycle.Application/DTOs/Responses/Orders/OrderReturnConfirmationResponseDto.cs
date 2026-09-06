using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderReturnConfirmationResponseDto
    {
        public Guid OrderId { get; set; }
        public Guid DisputeId { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DisputeStatus DisputeStatus { get; set; }
        public DateTime? BuyerReturnConfirmedAt { get; set; }
        public DateTime? SellerReturnReceivedAt { get; set; }
        public DateTime? ReturnDueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public decimal RefundedAmount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
