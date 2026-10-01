using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeResponseDto
    {
        public Guid DisputeResponseId { get; set; }

        public DisputeResponseType ResponseType { get; set; }

        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DisputeUserSummaryDto Responder { get; set; } = null!;

        public IReadOnlyList<MediaResponse> EvidenceImages { get; set; } =
            Array.Empty<MediaResponse>();
    }
}
