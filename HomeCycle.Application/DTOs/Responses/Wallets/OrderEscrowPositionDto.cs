using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public sealed class OrderEscrowPositionDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }

        public Guid BuyerId { get; set; }
        public string BuyerUsername { get; set; } = string.Empty;

        public Guid SellerId { get; set; }
        public string SellerUsername { get; set; } = string.Empty;

        public decimal EscrowAmount { get; set; }

        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public DateTime? DisputeWindowEndsAt { get; set; }
        public bool HasActiveDispute { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
