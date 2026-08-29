using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Configs
{
    public class AppointmentPolicyConfigDto
    {
        public int CheckInOpenBeforeMinutes { get; set; }

        public int NoInteractionExpiryMinutes { get; set; }

        public int RescheduleCutoffHours { get; set; }

        public int CancellationCutoffHours { get; set; }
    }

}
