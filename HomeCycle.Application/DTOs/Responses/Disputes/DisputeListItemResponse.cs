using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeListItemResponse
    {
        public Guid DisputeId { get; set; }

        public Guid SenderId { get; set; }

        public string SenderUsername { get; set; } = string.Empty;

        public Guid? TargetUserId { get; set; }

        public string? TargetUsername { get; set; }

        public DisputeTargetType? TargetType { get; set; }

        public Guid? TargetId { get; set; }

        public string? OrderCode { get; set; }

        public DisputeCategory? Category { get; set; }

        public DisputeStatus? Status { get; set; }

        public string? Description { get; set; }
        public DisputeResolutionOutcome? ResolutionOutcome { get; set; }
        public DateTime? ReturnDueAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
