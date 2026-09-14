using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public class WalletTransactionSearchRequest : PaginationRequest
    {
        public TransactionType? TransactionType { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public WalletTransactionStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ReferenceId { get; set; }
        public Guid? WalletId { get; set; }
        public Guid? UserId { get; set; }
    }
}
