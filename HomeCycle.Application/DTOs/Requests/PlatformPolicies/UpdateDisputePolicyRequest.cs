using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.PlatformPolicies
{
    public class UpdateDisputePolicyRequest
    {
        public int? NormalDisputeWindowDays { get; set; }

        public int? LowReputationDisputeWindowDays { get; set; }

        public int? LowReputationThreshold { get; set; }

        public int? ReturnWindowDays { get; set; }

        public int? DisputeLossPenaltyPoints { get; set; }
    }
}
