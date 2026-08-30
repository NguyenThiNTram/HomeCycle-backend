using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentActionResponseDto
    {
        public Guid AppointmentId { get; set; }

        public AppointmentStatus? AppointmentStatus { get; set; }

        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
