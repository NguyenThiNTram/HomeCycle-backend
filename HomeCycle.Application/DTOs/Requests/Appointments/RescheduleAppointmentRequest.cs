using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Appointments
{
    public class RescheduleAppointmentRequest
    {
        public DateTimeOffset ProposedAt { get; set; }
    }
}
