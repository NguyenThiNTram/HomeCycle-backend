using HomeCycle.Application.Pricing.Services;

namespace HomeCycle.Application.Pricing.Models;

// Giá bán mới của đúng model (hoặc biến thể hậu tố) ở các nhà bán lẻ, tìm qua Google.
public sealed record NewPriceSearchResult(
    IReadOnlyList<NewPriceEvidence> Items,
    bool FromCache)
{
    public static NewPriceSearchResult Empty { get; } = new([], false);
}

public sealed record NewPriceEvidence(
    decimal PriceVnd,
    string ObservedModel,
    string SourceName,
    string SourceUrl,
    DateTimeOffset RetrievedAt,
    PriceSourceTier SourceTier = PriceSourceTier.Untrusted,
    string? SourceDomain = null);
