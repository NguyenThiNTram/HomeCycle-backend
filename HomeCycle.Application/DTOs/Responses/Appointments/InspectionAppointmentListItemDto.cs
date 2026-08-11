using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class InspectionAppointmentListItemDto
    {
        public Guid AppointmentId { get; set; }
        public int? AppointmentStatus { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? InspectionAddress { get; set; }
        public bool IsCancelled { get; set; }
        public bool BuyerCheckedIn { get; set; }
        public bool SellerCheckedIn { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ProductName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? CounterpartyName { get; set; }
    }
}
