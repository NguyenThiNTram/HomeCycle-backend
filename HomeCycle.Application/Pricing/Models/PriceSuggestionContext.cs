namespace HomeCycle.Application.Pricing.Models;

public sealed record PriceSuggestionContext(
    PriceSuggestionProduct Product,
    PriceTimeGroupedStatistics CompletedTrades,
    PriceStatistics ActiveListings,
    PriceStatistics ExternalUsedListings,
    PriceStatistics EquivalentCompletedTrades,
    PriceStatistics EquivalentActiveListings,
    PriceStatistics EquivalentExternalUsedListings,
    PriceStatistics NewMarketPrices,
    AllowedPriceRange AllowedPriceRange,
    PriceEvidenceQuality EvidenceQuality);

public sealed record PriceSuggestionProduct(
    string ProductType,
    string Brand,
    string Model,
    string Functionality,
    string Damage,
    int? UsageMonths,
    IReadOnlyList<PriceContextAttribute> Attributes);

public sealed record PriceContextAttribute(string Name, string? Value, string? Unit);

public sealed record PriceTimeGroupedStatistics(
    PriceStatistics Recent0To30Days,
    PriceStatistics Older31To90Days,
    PriceStatistics All,
    int ExactConditionSamples);

public sealed record PriceStatistics(
    int SampleCount,
    decimal? MedianPrice,
    decimal? MinPrice,
    decimal? MaxPrice);

public sealed record AllowedPriceRange(decimal MinPrice, decimal MaxPrice);

public sealed record PriceEvidenceQuality(
    int MatchedSamples,
    int UnknownAttributeSamples,
    bool UsedExternalSearch,
    bool ExternalSearchFromCache);

public sealed record AiPriceDecision(
    decimal? SuggestedPrice,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyList<string> ReasonCodes,
    string ShortExplanation);
