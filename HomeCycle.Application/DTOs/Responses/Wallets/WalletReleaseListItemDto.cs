using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WalletReleaseListItemDto
    {
        public Guid WalletTransactionId { get; set; }

        public Guid? FromWalletId { get; set; }
        public Guid? ToWalletId { get; set; }

        public Guid? ReferenceId { get; set; }
        public ReferenceType? ReferenceType { get; set; }

        public decimal? Amount { get; set; }

        public WalletTransactionStatus? Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
