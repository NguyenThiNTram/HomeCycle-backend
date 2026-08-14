using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class SystemWalletSummaryDto
    {
        // Chi tiết từng ví hệ thống theo Purpose (Shipping_Escrow, Platform_Revenue, ...)
        public List<WalletInfoDto> Wallets { get; set; } = new();

        public decimal TotalAvailableBalance { get; set; }
        public decimal TotalHoldBalance { get; set; }

        // Tổng tiền hệ thống đang giữ hộ = Available + Hold, cộng dồn toàn bộ ví hệ thống
        public decimal TotalHeldBalance { get; set; }
    }
}
