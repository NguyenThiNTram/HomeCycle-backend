using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class inspection_form
{

    public Guid InspectionFormId { get; set; }
    public Guid InspectionAppointmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid InspectorId { get; set; }

    public DateTime? InspectionTime { get; set; }

    public int? OperatingStatus { get; set; }
    public int? AppearanceStatus { get; set; }
    public int? PartsStatus { get; set; }
    public int? MatchStatus { get; set; }
    public string? InspectorNotes { get; set; }

    public int? Conclusion { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? SuggestedPrice { get; set; }

    public int? CollectAction { get; set; }

    public int? InspectionStatus { get; set; }
    public int Revision { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? SellerDecisionAt { get; set; }
    public string? SellerDecisionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public inspection_form()
    {
    }

    public inspection_form(Guid InspectionFormId, Guid InspectionAppointmentId, Guid InspectorId)
    {
        this.InspectionFormId = InspectionFormId;
        this.InspectionAppointmentId = InspectionAppointmentId;
        this.InspectorId = InspectorId;
    }
}
