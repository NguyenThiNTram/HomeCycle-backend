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

        public decimal TotalSystemAvailable { get; set; }
        public decimal TotalSystemHold { get; set; }

        public decimal TotalRecordedBalance { get; set; }

        public List<WalletInfoDto> SystemWallets { get; set; } = new();
    }
}
