using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public sealed class OrderEscrowSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public bool? HasActiveDispute { get; set; }
    }
}
