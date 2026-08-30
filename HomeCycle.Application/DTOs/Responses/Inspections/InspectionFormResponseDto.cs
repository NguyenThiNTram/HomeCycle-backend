using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Inspections
{

    public class InspectionFormResponseDto
    {
        public Guid InspectionFormId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid InspectionAppointmentId { get; set; }
        public Guid OrderId { get; set; }
        public Guid InspectorId { get; set; }

        public int Revision { get; set; }
        public InspectionStatus InspectionStatus { get; set; }

        public DateTime? InspectionTime { get; set; }

        public InspectionOperatingStatus? OperatingStatus { get; set; }
        public InspectionAppearanceStatus? AppearanceStatus { get; set; }
        public InspectionPartsStatus? PartsStatus { get; set; }
        public InspectionMatchStatus? MatchStatus { get; set; }

        public string? InspectorNotes { get; set; }

        public InspectionConclusion? Conclusion { get; set; }

        public decimal? OriginalPrice { get; set; }
        public decimal? SuggestedPrice { get; set; }

        public InspectionCollectAction? CollectAction { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public DateTime? SellerDecisionAt { get; set; }
        public string? SellerDecisionReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public IReadOnlyList<MediaResponse> Images { get; set; } = Array.Empty<MediaResponse>();

        public InspectionOrderSummaryDto Order { get; set; } = new();
        public InspectionFormActionDto Actions { get; set; } = new();
    }

    public class InspectionOrderSummaryDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public decimal? OriginalTotalAmount { get; set; }
        public decimal? FinalTotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountRemaining { get; set; }
    }

    public class InspectionFormActionDto
    {
        public bool CanEdit { get; set; }
        public bool CanSubmit { get; set; }

        public bool CanSellerConfirm { get; set; }
        public bool CanSellerReject { get; set; }

        public bool CanCollectNow { get; set; }
        public bool CanCancelTransaction { get; set; }
    }
}
