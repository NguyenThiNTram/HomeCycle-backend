using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public sealed class OrderTimelineStepDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public OrderTimelineStepStatus Status { get; set; }
        public DateTime? OccurredAt { get; set; }
        public IReadOnlyList<OrderTimelineStepDto> SubSteps { get; set; } = Array.Empty<OrderTimelineStepDto>();
    }
}

