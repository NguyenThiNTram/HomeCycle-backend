using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletFinanceFundsDto
    {
        public decimal TotalUserAvailable { get; set; }
        public decimal TotalUserHold { get; set; }
        public decimal TotalPersonalAvailable { get; set; }
        public decimal TotalPersonalHold { get; set; }
        public decimal TotalBusinessAvailable { get; set; }
        public decimal TotalBusinessHold { get; set; }
        public decimal TotalSystemBalance { get; set; }
        public decimal TotalRecordedBalance { get; set; }
        public List<SystemWalletBalanceDto> SystemWallets { get; set; } = new();
    }
}
