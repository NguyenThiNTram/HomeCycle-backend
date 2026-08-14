using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public class PaymentHistoryResponseDto
    {
        public Guid PaymentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        // Để FE điều hướng vào chi tiết đơn hàng khi bấm vào dòng lịch sử (giống "Order #1 >" trong app tham khảo)
        public Guid? OrderId { get; set; }

    }
}
