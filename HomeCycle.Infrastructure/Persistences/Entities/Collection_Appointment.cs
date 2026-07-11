using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Collection_Appointment")]
[Index("AppointmentId", Name = "Collection_Appointment_AppointmentId_key", IsUnique = true)]
[Index("AppointmentId", Name = "idx_collection_appointment")]
public partial class Collection_Appointment
{
    [Key]
    public Guid CollectionAppointmentId { get; set; }

    public Guid AppointmentId { get; set; }

    public DateTime? CollectionDate { get; set; }

    public string? PickupAddress { get; set; }

    public string? DeliveryAddress { get; set; }

    [StringLength(50)]
    public string? DeliveryMethod { get; set; }

    [ForeignKey("AppointmentId")]
    [InverseProperty("Collection_Appointment")]
    public virtual Appointment Appointment { get; set; } = null!;

    [InverseProperty("CollectionAppointment")]
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
