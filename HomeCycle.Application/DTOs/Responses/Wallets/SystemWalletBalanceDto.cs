using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class SystemWalletBalanceDto
    {
        public Guid WalletId { get; set; }
        public SystemWalletPurpose? Purpose { get; set; }
        public decimal Balance { get; set; }
    }
}
