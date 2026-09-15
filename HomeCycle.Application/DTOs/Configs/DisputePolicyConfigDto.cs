using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Configs
{
    public class DisputePolicyConfigDto
    {
        public int NormalDisputeWindowDays { get; set; }

        public int LowReputationDisputeWindowDays { get; set; }

        public int LowReputationThreshold { get; set; }

        public int ReturnWindowDays { get; set; }

        public int DisputeLossPenaltyPoints { get; set; }

        // Defaults also apply to existing policy JSON that predates content reporting.
        public int PostViolationPenaltyPoints { get; set; } = 10;
        public int ReviewViolationPenaltyPoints { get; set; } = 5;
    }

    public class RatingPolicyConfigDto
    {
        public double PriorMean { get; set; } = 4.0;
        public int PriorWeight { get; set; } = 5;
        public int FiveStarPoints { get; set; } = 2;
        public int FourStarPoints { get; set; } = 1;
        public int ThreeStarPoints { get; set; }
        public int TwoStarPoints { get; set; } = -2;
        public int OneStarPoints { get; set; } = -5;
        public int MinimumReputationScore { get; set; }
        public int MaximumReputationScore { get; set; } = 100;
        public int ReviewEditWindowDays { get; set; } = 3;
    }
}
