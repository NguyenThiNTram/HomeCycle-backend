using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.Disputes;

public sealed class PostDisputeSummaryDto
{
    public Guid PostId { get; set; }
    public Guid OwnerId { get; set; }
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public decimal? BasePrice { get; set; }
    public PostType? PostType { get; set; }
    public PostStatus? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<MediaResponse> Images { get; set; } = Array.Empty<MediaResponse>();
}

public sealed class ReviewDisputeSummaryDto
{
    public Guid ReviewId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid RevieweeId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewStatus? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<MediaResponse> Images { get; set; } = Array.Empty<MediaResponse>();
}
