namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchCachedDecision(
    Guid SellPostId,
    int Rank,
    decimal? AiScore,
    IReadOnlyList<string> ReasonCodes,
    string? ShortExplanation);

public sealed record SupplierMatchCacheEntry(
    IReadOnlyList<SupplierMatchCachedDecision> Decisions,
    DateTimeOffset CreatedAt,
    string RankingSource,
    string AiStatus);

public sealed record SupplierCandidateLiveState(
    Guid SellPostId,
    bool IsActive,
    int AvailableQuantity,
    decimal? Price);
