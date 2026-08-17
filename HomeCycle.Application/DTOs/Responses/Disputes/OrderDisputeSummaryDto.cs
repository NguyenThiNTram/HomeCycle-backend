using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class OrderDisputeSummaryDto
    {
        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public Guid PostId { get; set; }

        public string? ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal? FinalTotalAmount { get; set; }

        public OrderStatus? OrderStatus { get; set; }

        public PaymentStatus? PaymentStatus { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Mốc cuối cùng được phép tạo dispute.
        /// Null khi giao dịch vẫn đang diễn ra và chưa có mốc delivery/completion.
        /// </summary>
        public DateTime? DisputeDeadlineUtc { get; set; }

        public int DisputeWindowHours { get; set; }
    }
}
