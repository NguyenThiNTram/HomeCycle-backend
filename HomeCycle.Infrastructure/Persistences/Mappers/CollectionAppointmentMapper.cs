using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class CollectionAppointmentMapper
    {
        public static collection_appointment ToDomain(this Collection_Appointment entity)
        {
            if (entity == null) return null;
            return new collection_appointment
            {
                CollectionAppointmentId = entity.CollectionAppointmentId,
                AppointmentId = entity.AppointmentId,
                CollectionDate = entity.CollectionDate,
                PickupAddress = entity.PickupAddress,
                DeliveryAddress = entity.DeliveryAddress,
                DeliveryMethod = entity.DeliveryMethod
            };
        }
        public static Collection_Appointment ToInfrastructure(this collection_appointment entity)
        {
            if (entity == null) return null;
            return new Collection_Appointment
            {
                CollectionAppointmentId = entity.CollectionAppointmentId,
                AppointmentId = entity.AppointmentId,
                CollectionDate = entity.CollectionDate,
                PickupAddress = entity.PickupAddress,
                DeliveryAddress = entity.DeliveryAddress,
                DeliveryMethod = entity.DeliveryMethod
            };
        }
    }
}
