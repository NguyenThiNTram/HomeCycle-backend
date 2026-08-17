using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class CreateDisputeResponse
    {
        public Guid DisputeId { get; set; }

        public DisputeStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public IReadOnlyList<MediaResponse> EvidenceImages { get; set; }
            = Array.Empty<MediaResponse>();
    }
}
