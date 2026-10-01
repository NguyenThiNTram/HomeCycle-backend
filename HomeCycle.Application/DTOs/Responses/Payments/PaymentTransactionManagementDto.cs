using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public sealed class PaymentTransactionManagementDto
    {
        public Guid PaymentTransactionId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }

        public string? PayOSOrderCode { get; set; }
        public string? PayOSPaymentLinkId { get; set; }
        public string? PayOSTransactionId { get; set; }
        public string? CheckoutUrl { get; set; }

        public PaymentTransactionStatus? PaymentTransactionStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
