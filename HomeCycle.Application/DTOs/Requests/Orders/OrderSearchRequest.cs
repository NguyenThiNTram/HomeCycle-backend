using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Orders
{
    public class OrderSearchRequest : PaginationRequest
    {

        public string? Keyword { get; set; }

        public OrderStatus? Status { get; set; }
    }
}
