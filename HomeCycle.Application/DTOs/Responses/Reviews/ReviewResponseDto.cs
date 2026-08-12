using HomeCycle.Application.DTOs.Responses.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Reviews
{
    public class ReviewResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ReviewerId { get; set; }
        public Guid RevieweeId { get; set; }

        public int? Rating { get; set; }
        public string? Comment { get; set; }

        public int? ReviewStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool CanEdit { get; set; }

        public string? ReviewerName { get; set; }
        public string? ReviewerAvatarUrl { get; set; }
        public string? RevieweeName { get; set; }
        public string? RevieweeAvatarUrl { get; set; }

        public IReadOnlyList<MediaResponse> Images { get; set; } = new List<MediaResponse>();
    }
}
