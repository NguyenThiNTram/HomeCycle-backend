using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Orders
{
    public class ModeratorOrderSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }

        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public Guid? BuyerId { get; set; }
        public Guid? SellerId { get; set; }

        public bool? HasActiveDispute { get; set; }
        public bool? HasInspection { get; set; }

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
