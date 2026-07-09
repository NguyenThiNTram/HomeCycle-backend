using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public partial class collection_appointment
{
    public Guid CollectionAppointmentId { get; set; }
    public Guid AppointmentId { get; set; }

    public DateTime? CollectionDate { get; set; }
    public string? PickupAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryMethod { get; set; }

    public collection_appointment()
    {
    }

    public collection_appointment(Guid CollectionAppointmentId, Guid AppointmentId)
    {
        this.CollectionAppointmentId = CollectionAppointmentId;
        this.AppointmentId = AppointmentId;
    }
}
