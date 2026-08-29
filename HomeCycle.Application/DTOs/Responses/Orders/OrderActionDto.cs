using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderActionDto
    {
        public bool CanConfirm { get; set; }
        public OrderConfirmAction? ConfirmAction { get; set; }
        public bool CanReview { get; set; }
        public bool CanDispute { get; set; }
        public IReadOnlyList<DisputeCategory> AllowedDisputeCategories { get; set; } = Array.Empty<DisputeCategory>();
    }
}
