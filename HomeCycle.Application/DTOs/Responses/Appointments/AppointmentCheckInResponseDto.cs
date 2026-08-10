using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentCheckInResponseDto
    {
        public Guid AppointmentId { get; set; }
        public int? AppointmentStatus { get; set; }
        public DateTime? BuyerCheckAt { get; set; }
        public DateTime? SellerCheckAt { get; set; }
        public bool IsFullyCheckedIn => BuyerCheckAt.HasValue && SellerCheckAt.HasValue;
    }
}
