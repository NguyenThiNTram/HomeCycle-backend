using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentSummaryDto
    {
        public Guid AppointmentId { get; set; }

        public AppointmentType? AppointmentType { get; set; }
        public AppointmentStatus? AppointmentStatus { get; set; }

        public DateTime? ScheduledAt { get; set; }
        public string? Location { get; set; }

        public DateTime? BuyerCheckAt { get; set; }
        public DateTime? SellerCheckAt { get; set; }

        public DateTime? InteractionDeadlineAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
