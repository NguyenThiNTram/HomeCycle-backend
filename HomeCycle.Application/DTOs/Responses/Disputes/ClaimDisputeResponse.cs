using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class ClaimDisputeResponse
    {
        public Guid DisputeId { get; set; }
        public DisputeStatus Status { get; set; }
        public Guid ModeratorId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
