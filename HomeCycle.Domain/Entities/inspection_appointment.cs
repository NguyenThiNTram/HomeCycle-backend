using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class inspection_appointment
{
    public Guid InspectionAppointmentId { get; set; }
    public Guid AppointmentId { get; set; }

    public string? InspectionAddress { get; set; }

    public DateTime? InspectionDate { get; set; }

    public inspection_appointment()
    {
    }

    public inspection_appointment(Guid InspectionAppointmentId, Guid AppointmentId)
    {
        this.InspectionAppointmentId = InspectionAppointmentId;
        this.AppointmentId = AppointmentId;
    }
}
