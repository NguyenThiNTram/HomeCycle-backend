using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeAppointmentContextDto
    {
        public Guid AppointmentId { get; set; }

        public AppointmentType? AppointmentType { get; set; }

        public AppointmentStatus? AppointmentStatus { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public string? Location { get; set; }

        public DateTime? BuyerCheckAt { get; set; }

        public DateTime? SellerCheckAt { get; set; }

        public DateTime? LateThresholdAt { get; set; }
    }

    public class DisputeInspectionContextDto
    {
        public Guid InspectionFormId { get; set; }

        public InspectionMode InspectionMode { get; set; }

        public InspectionStatus? InspectionStatus { get; set; }

        public InspectionOperatingStatus? OperatingStatus { get; set; }

        public InspectionAppearanceStatus? AppearanceStatus { get; set; }

        public InspectionPartsStatus? PartsStatus { get; set; }

        public InspectionMatchStatus? MatchStatus { get; set; }

        public string? InspectorNotes { get; set; }

        public InspectionConclusion? Conclusion { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? SellerDecisionAt { get; set; }

        public string? SellerDecisionReason { get; set; }

        public IReadOnlyList<MediaResponse> Images { get; set; } =
            Array.Empty<MediaResponse>();
    }
}
