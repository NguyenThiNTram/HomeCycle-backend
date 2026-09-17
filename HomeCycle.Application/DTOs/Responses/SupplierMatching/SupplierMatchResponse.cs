using HomeCycle.Application.DTOs.Responses.Posts;

namespace HomeCycle.Application.DTOs.Responses.SupplierMatching;

public sealed class SupplierMatchResponse
{
    public Guid? BuyPostId { get; set; }
    public string Tier { get; set; } = "FREE";
    public string RankingSource { get; set; } = "BACKEND_ONLY";
    public string AiStatus { get; set; } = "NOT_ENABLED";
    public bool AdvancedFiltersApplied { get; set; }
    public int ResultLimit { get; set; } = 5;
    public int RemainingAiRefreshes { get; set; }
    public DateTimeOffset ResetsAt { get; set; }
    public bool FromCache { get; set; }
    public int CandidateCount { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public IReadOnlyList<BuyPostMatchResponse> Matches { get; set; } = [];
}
