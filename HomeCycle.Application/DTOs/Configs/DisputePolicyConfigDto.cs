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
    }
}
