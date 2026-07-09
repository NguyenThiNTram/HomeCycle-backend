using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class shipment
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? CollectionAppointmentId { get; set; }

    public int? DeliveryMethod { get; set; }

    public int? ShipmentStatus { get; set; }

    public string? FromName { get; set; }
    public string? FromPhone { get; set; }
    public string? PickupAddress { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public string? ToName { get; set; }
    public string? ToPhone { get; set; }
    public string? DeliveryAddress { get; set; }

    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public shipment()
    {
    }

    public shipment(Guid ShipmentId, Guid OrderId)
    {
        this.ShipmentId = ShipmentId;
        this.OrderId = OrderId;
    }
}
