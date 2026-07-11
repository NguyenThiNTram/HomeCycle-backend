using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class inspection_form
{

    public Guid InspectionFormId { get; set; }
    public Guid InspectionAppointmentId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid InspectorId { get; set; }

    public DateTime? InspectionTime { get; set; }

    public string? OperatingStatus { get; set; }
    public string? AppearanceStatus { get; set; }
    public string? PartsStatus { get; set; }
    public string? MatchStatus { get; set; }
    public string? InspectorNotes { get; set; }

    public string? Conclusion { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? SuggestedPrice { get; set; }

    public string? CollectAction { get; set; }

    public int? InspectionStatus { get; set; }

    public DateTime CreatedAt { get; set; }

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
