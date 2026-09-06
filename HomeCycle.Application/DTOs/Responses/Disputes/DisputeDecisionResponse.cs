using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeDecisionResponse
    {
        public Guid DisputeId { get; set; }
        public DisputeStatus Status { get; set; }
        public Guid ModeratorId { get; set; }
        public string ModeratorNote { get; set; } = string.Empty;
        public DisputeResolutionOutcome? ResolutionOutcome { get; set; }
        public Guid? OrderId { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public decimal RefundedAmount { get; set; }
        public DateTime? ReturnDueAt { get; set; }
        public DateTime ResolvedAt { get; set; }
    }
}
