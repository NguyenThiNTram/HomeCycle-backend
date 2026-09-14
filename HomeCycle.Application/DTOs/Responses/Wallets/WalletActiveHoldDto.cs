using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletActiveHoldDto
    {
        public Guid WalletId { get; set; }

        public ReferenceType? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }

        public decimal HoldAmount { get; set; }
    }
}
