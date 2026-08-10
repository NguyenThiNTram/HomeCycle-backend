using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentDetailDto
    {
        public appointment Appointment { get; set; } = null!;
        public inspection_appointment? InspectionAppointment { get; set; }
        public collection_appointment? CollectionAppointment { get; set; }
    }

}
