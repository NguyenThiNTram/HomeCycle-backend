using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public sealed class OrderTrackingUpdatedResponse
    {
        public Guid OrderId { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
