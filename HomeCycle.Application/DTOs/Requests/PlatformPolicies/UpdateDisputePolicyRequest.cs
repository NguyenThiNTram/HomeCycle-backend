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
        public int? PostViolationPenaltyPoints { get; set; }
        public int? ReviewViolationPenaltyPoints { get; set; }
    }

    public class UpdateRatingPolicyRequest
    {
        public double? PriorMean { get; set; }
        public int? PriorWeight { get; set; }
        public int? FiveStarPoints { get; set; }
        public int? FourStarPoints { get; set; }
        public int? ThreeStarPoints { get; set; }
        public int? TwoStarPoints { get; set; }
        public int? OneStarPoints { get; set; }
        public int? MinimumReputationScore { get; set; }
        public int? MaximumReputationScore { get; set; }
        public int? ReviewEditWindowDays { get; set; }
    }
}
