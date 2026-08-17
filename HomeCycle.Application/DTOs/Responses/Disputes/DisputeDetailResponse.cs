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

        public DisputeUserSummaryDto Sender { get; set; } = null!;

        public DisputeUserSummaryDto? TargetUser { get; set; }

        public DisputeTargetSummaryDto Target { get; set; } = null!;

        public DisputeCategory? Category { get; set; }

        public string? Description { get; set; }

        public DisputeStatus? Status { get; set; }

        public Guid? ModeratorId { get; set; }

        public string? ModeratorNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public IReadOnlyList<MediaResponse> EvidenceImages { get; set; }
            = Array.Empty<MediaResponse>();
    }
}
