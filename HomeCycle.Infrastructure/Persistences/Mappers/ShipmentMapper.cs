using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ShipmentMapper
    {
        public static shipment ToDomain(this Shipment entity)
        {
            if (entity == null) return null;
            return new shipment
            {
                ShipmentId = entity.ShipmentId,
                OrderId = entity.OrderId,
                CollectionAppointmentId = entity.CollectionAppointmentId,
                DeliveryMethod = entity.DeliveryMethod,
                ShipmentStatus = entity.ShipmentStatus,
                FromName = entity.FromName,
                FromPhone = entity.FromPhone,
                PickupAddress = entity.PickupAddress,
                PickedUpAt = entity.PickedUpAt,
                ToName = entity.ToName,
                ToPhone = entity.ToPhone,
                DeliveryAddress = entity.DeliveryAddress,
                DeliveredAt = entity.DeliveredAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Shipment ToInfrastructure(this shipment entity)
        {
            if (entity == null) return null;
            return new Shipment
            {
                ShipmentId = entity.ShipmentId,
                OrderId = entity.OrderId,
                CollectionAppointmentId = entity.CollectionAppointmentId,
                DeliveryMethod = entity.DeliveryMethod,
                ShipmentStatus = entity.ShipmentStatus,
                FromName = entity.FromName,
                FromPhone = entity.FromPhone,
                PickupAddress = entity.PickupAddress,
                PickedUpAt = entity.PickedUpAt,
                ToName = entity.ToName,
                ToPhone = entity.ToPhone,
                DeliveryAddress = entity.DeliveryAddress,
                DeliveredAt = entity.DeliveredAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
