using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.PlatformPolicies
{
    public class UpdateAppointmentPolicyRequest
    {
        public int? CheckInOpenBeforeMinutes { get; set; }

        public int? LateThresholdMinutes { get; set; }

        public int? RescheduleCutoffHours { get; set; }

        public int? CancellationCutoffHours { get; set; }
    }
}
