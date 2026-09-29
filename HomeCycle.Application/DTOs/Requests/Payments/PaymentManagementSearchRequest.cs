using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Payments
{
    public sealed class PaymentManagementSearchRequest : PaginationRequest
    {
        public Guid? PayerId { get; set; }
        public PaymentType? Type { get; set; }
        public PaymentMethod? Method { get; set; }
        public PaymentStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
