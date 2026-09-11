using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public class WithdrawalSearchRequest : PaginationRequest
    {
        public WithdrawalStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public sealed class WithdrawalQuery
    {
        public Guid? UserId { get; init; }
        public string? Keyword { get; init; }
        public WithdrawalStatus? Status { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public bool PrioritizeActionable { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
    }
}
