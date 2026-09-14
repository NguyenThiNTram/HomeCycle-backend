using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletFinancePartyDto
    {
        public Guid WalletId { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public UserRole? Role { get; set; }
        public WalletTypeEnum WalletType { get; set; }
        public SystemWalletPurpose? SystemPurpose { get; set; }
    }
}
