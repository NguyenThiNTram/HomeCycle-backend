using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class appointment
{
    public Guid AppointmentId { get; set; }
    public Guid AgreementId { get; set; }

    public int? AppointmentType { get; set; }
    public int? AppointmentStatus { get; set; }

    public DateTime? BuyerCheckAt { get; set; }
    public DateTime? SellerCheckAt { get; set; }

    public DateTime? InteractionDeadlineAt { get; set; }

    public Guid? RescheduledFromAppointmentId { get; set; }
    public Guid? RescheduleRequestedByUserId { get; set; }
    public DateTime? RescheduleRequestedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public appointment()
    {
    }

    public appointment(Guid AppointmentId, Guid AgreementId)
    {
        this.AppointmentId = AppointmentId;
        this.AgreementId = AgreementId;
    }

}
