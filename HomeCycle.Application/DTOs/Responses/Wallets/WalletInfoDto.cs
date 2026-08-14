using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletInfoDto
    {
        public Guid WalletId { get; set; }
        public WalletTypeEnum WalletType { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal HoldBalance { get; set; }
        public SystemWalletPurpose? Purpose { get; set; } // Dành cho System Wallet
    }
}
