using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class InspectionAppointmentMapper
    {
        public static inspection_appointment ToDomain(this Inspection_Appointment entity)
        {
            return new inspection_appointment
            {
                InspectionAppointmentId = entity.InspectionAppointmentId,
                AppointmentId = entity.AppointmentId,
                InspectionAddress = entity.InspectionAddress,
                InspectionDate = entity.InspectionDate
            };
        }
        public static Inspection_Appointment ToInfrastructure(this inspection_appointment entity)
        {
            return new Inspection_Appointment
            {
                InspectionAppointmentId = entity.InspectionAppointmentId,
                AppointmentId = entity.AppointmentId,
                InspectionAddress = entity.InspectionAddress,
                InspectionDate = entity.InspectionDate
            };
        }
    }
}
