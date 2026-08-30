using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderCancellationResponseDto
    {
        public Guid OrderId { get; set; }

        public OrderStatus OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public DateTime CancelledAt { get; set; }
        public Guid? CancelledByUserId { get; set; }

        public string? CancellationReason { get; set; }
    }
}
