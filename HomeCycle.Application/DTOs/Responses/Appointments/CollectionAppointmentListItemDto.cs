using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class CollectionAppointmentListItemDto
    {
        public Guid AppointmentId { get; set; }
        public int? AppointmentStatus { get; set; }
        public DateTime? CollectionDate { get; set; }
        public string? PickupAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryMethod { get; set; }

        public DateTime? LateThresholdAt { get; set; }
        public bool IsOverdue { get; set; }


        public bool IsCancelled { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CounterpartyName { get; set; }
    }
}
