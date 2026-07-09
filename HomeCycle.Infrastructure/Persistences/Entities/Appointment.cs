using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Appointment")]
[Index("AgreementId", Name = "idx_appointment_agreement")]
[Index("AppointmentStatus", Name = "idx_appointment_status")]
public partial class Appointment
{
    [Key]
    public Guid AppointmentId { get; set; }

    public Guid AgreementId { get; set; }

    public int? AppointmentType { get; set; }

    public int? AppointmentStatus { get; set; }

    public DateTime? BuyerCheckAt { get; set; }

    public DateTime? SellerCheckAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AgreementId")]
    [InverseProperty("Appointments")]
    public virtual Agreement_Form Agreement { get; set; } = null!;

    [InverseProperty("Appointment")]
    public virtual Collection_Appointment? Collection_Appointment { get; set; }

    [InverseProperty("Appointment")]
    public virtual Inspection_Appointment? Inspection_Appointment { get; set; }
}
