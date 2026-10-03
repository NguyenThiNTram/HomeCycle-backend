using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeDetailResponse
    {
        public Guid DisputeId { get; set; }

        public DisputeUserSummaryDto? Sender { get; set; } = null!;

        public DisputeUserSummaryDto? TargetUser { get; set; }

        public DisputeTargetSummaryDto Target { get; set; } = null!;

        public DisputeCategoryOptionDto? Category { get; set; }

        public DisputeAppointmentContextDto? AppointmentContext { get; set; }

        public DisputeInspectionContextDto? InspectionContext { get; set; }

        public string? Description { get; set; }

        public DisputeStatus? Status { get; set; }

        public Guid? ModeratorId { get; set; }
        public DisputeResolutionOutcome? ResolutionOutcome { get; set; }
        public DisputeOrigin Origin { get; set; }

        public DisputeResolutionOutcome? ProposedResolutionOutcome { get; set; }

        public DisputeResolutionSource? ResolutionSource { get; set; }

        public DateTime? ResponseDeadlineAt { get; set; }

        public DateTime? EscalatedAt { get; set; }

        public DateTime? ModeratorClaimedAt { get; set; }
        public string? ModeratorNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public IReadOnlyList<MediaResponse> EvidenceImages { get; set; }
            = Array.Empty<MediaResponse>();
        public DisputeActionDto Actions { get; set; } = new();

        public IReadOnlyList<DisputeResponseDto> Responses { get; set; } = Array.Empty<DisputeResponseDto>();
        public IReadOnlyList<DisputeTimelineStepDto> Timeline { get; set; } =
          Array.Empty<DisputeTimelineStepDto>();
    }

    public class DisputeActionDto
    {
        public bool CanCloseDispute { get; set; }
        public bool CanClaimDispute { get; set; }
        public bool CanResolveDispute { get; set; }
        public bool CanRejectDispute { get; set; }
        public bool CanVerifyReturn { get; set; }
        public bool CanAccept { get; set; }

        public bool CanRebut { get; set; }

        public bool CanSubmitStatement { get; set; }
    }
}
