namespace HomeCycle.Application.Pricing.Models;

public enum ExternalModelMatchLevel
{
    Unrelated = 0,
    Related = 1,
    Variant = 2,
    Exact = 3
}

public sealed record ExternalUsedPriceSearchResult(
    IReadOnlyList<ExternalUsedPriceEvidence> Items,
    bool IsGrounded,
    bool FromCache)
{
    public static ExternalUsedPriceSearchResult Empty { get; } = new([], false, false);
}

public sealed record ExternalUsedPriceEvidence(
    decimal PriceVnd,
    string ObservedModel,
    ExternalModelMatchLevel ModelMatchLevel,
    decimal ModelSimilarity,
    string? Condition,
    string SourceName,
    string SourceUrl,
    DateTimeOffset RetrievedAt);
