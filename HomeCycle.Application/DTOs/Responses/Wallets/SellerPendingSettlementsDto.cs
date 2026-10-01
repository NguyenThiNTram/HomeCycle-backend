using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public sealed class SellerPendingSettlementsDto
    {
        public decimal TotalPendingAmount { get; set; }
        public IReadOnlyList<SellerPendingSettlementItemDto> Items { get; set; } = Array.Empty<SellerPendingSettlementItemDto>();
    }
}
