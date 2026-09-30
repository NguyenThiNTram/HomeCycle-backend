using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public sealed class SellerPendingSettlementItemDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public decimal Amount { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
