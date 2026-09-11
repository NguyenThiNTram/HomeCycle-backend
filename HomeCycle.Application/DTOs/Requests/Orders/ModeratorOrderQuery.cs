using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Orders
{
    public sealed class ModeratorOrderQuery
    {
        public string? Keyword { get; init; }

        public OrderStatus? Status { get; init; }
        public PaymentStatus? PaymentStatus { get; init; }

        public Guid? BuyerId { get; init; }
        public Guid? SellerId { get; init; }

        public bool? HasActiveDispute { get; init; }
        public bool? HasInspection { get; init; }

        public DateTime? CreatedFrom { get; init; }
        public DateTime? CreatedTo { get; init; }

        public int PageNumber { get; init; }
        public int PageSize { get; init; }
    }

}
