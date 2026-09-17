using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Interfaces.Repositories.AI;

// Evidence only: no user identity, contact details, addresses, or payment totals.
public interface IPriceEvidenceRepository
{
    Task<IReadOnlyList<CompletedPriceEvidence>> GetCompletedAsync(Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingPriceEvidence>> GetActiveAsync(Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompletedPriceEvidence>> GetEquivalentCompletedAsync(Guid typeId, Guid brandId, string excludedModel, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingPriceEvidence>> GetEquivalentActiveAsync(Guid typeId, Guid brandId, string excludedModel, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketPriceEvidence>> GetVerifiedMarketAsync(Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken);
}

public sealed record CompletedPriceEvidence(
    Guid ProductId,
    string NormalizedModelNumber,
    int? FunctionalityStatus,
    int? DamageLevel,
    DateTime CompletedAt,
    decimal Price,
    IReadOnlyList<DynamicAttributeValue> Attributes);

public sealed record ListingPriceEvidence(
    Guid ProductId,
    string NormalizedModelNumber,
    int? FunctionalityStatus,
    int? DamageLevel,
    decimal Price,
    IReadOnlyList<DynamicAttributeValue> Attributes);
public sealed record MarketPriceEvidence(decimal PriceVndPerUnit, string SourceName, string SourceUrl,
    DateTime ObservedAt, bool HasVariants, string KeySpecifications);
