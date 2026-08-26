using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeTargetSummaryDto
    {
        public DisputeTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public OrderDisputeSummaryDto? Order { get; set; }

        // Sau này:
        // public ReviewDisputeSummaryDto? Review { get; set; }
    }
}
