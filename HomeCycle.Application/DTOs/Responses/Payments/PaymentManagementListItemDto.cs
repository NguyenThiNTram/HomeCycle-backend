using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public sealed class PaymentManagementListItemDto
    {
        public Guid PaymentId { get; set; }

        public Guid PayerId { get; set; }
        public string PayerUsername { get; set; } = string.Empty;

        public PaymentType? PaymentType { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;

        public Guid? AgreementId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? SubscriptionId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
