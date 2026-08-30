using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Inspection_Form")]
[Index("InspectionAppointmentId", Name = "Inspection_Form_InspectionAppointmentId_key", IsUnique = true)]
[Index("InspectorId", Name = "idx_inspection_inspector")]
[Index("OrderId", Name = "idx_inspection_order")]
[Index(nameof(InspectionStatus), Name = "idx_inspection_form_status")]
public partial class Inspection_Form
{
    [Key]
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

    [Precision(18, 2)]
    public decimal? OriginalPrice { get; set; }

    [Precision(18, 2)]
    public decimal? SuggestedPrice { get; set; }

    public int? CollectAction { get; set; }

    public int? InspectionStatus { get; set; }
    public int Revision { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? SellerDecisionAt { get; set; }
    public string? SellerDecisionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(InspectionAppointmentId))]
    [InverseProperty(nameof(Inspection_Appointment.Inspection_Form))]
    public virtual Inspection_Appointment InspectionAppointment { get; set; } = null!;

    [ForeignKey(nameof(InspectorId))]
    [InverseProperty(nameof(User.Inspection_Forms))]
    public virtual User Inspector { get; set; } = null!;

    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;
}
