using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class SystemWalletSummaryDto
    {
        public List<SystemWalletBalanceDto> Wallets { get; set; } = new();
        public decimal TotalBalance { get; set; }
    }
}
