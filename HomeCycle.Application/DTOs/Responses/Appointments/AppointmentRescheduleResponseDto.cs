using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentRescheduleResponseDto
    {
        public Guid OriginalAppointmentId { get; set; }
        public Guid ProposalAppointmentId { get; set; }

        public AppointmentStatus OriginalStatus { get; set; }
        public AppointmentStatus ProposalStatus { get; set; }

        public DateTime ProposedAt { get; set; }

        public Guid RequestedByUserId { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
