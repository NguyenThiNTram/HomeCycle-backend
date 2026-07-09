using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Inspection_Appointment")]
[Index("AppointmentId", Name = "Inspection_Appointment_AppointmentId_key", IsUnique = true)]
[Index("AppointmentId", Name = "idx_inspection_appointment")]
public partial class Inspection_Appointment
{
    [Key]
    public Guid InspectionAppointmentId { get; set; }

    public Guid AppointmentId { get; set; }

    public string? InspectionAddress { get; set; }

    public DateTime? InspectionDate { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("Inspection_Appointment")]
    public virtual Appointment Appointment { get; set; } = null!;

    [InverseProperty("InspectionAppointment")]
    public virtual Inspection_Form? Inspection_Form { get; set; }
}
