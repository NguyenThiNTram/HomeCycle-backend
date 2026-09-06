using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class CloseDisputeResponse
    {
        public Guid DisputeId { get; set; }

        public DisputeStatus DisputeStatus { get; set; }

        public Guid? OrderId { get; set; }

        public OrderStatus? RestoredOrderStatus { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
