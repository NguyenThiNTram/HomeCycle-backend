using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public class WalletLedgerSearchRequest : PaginationRequest
    {
        // Cố tình bỏ Keyword vì không thể search text full-scan trên bảng Ledger hàng triệu dòng
        public LedgerDirection? Direction { get; set; } // 0: In, 1:
        public BalanceType? BalanceType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
