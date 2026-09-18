namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchAiCandidate(
    string Alias,
    decimal BackendScore,
    string ModelMatchLevel,
    decimal? PriceFit,
    decimal QuantityFit,
    int AttributeMatched,
    int AttributeConflicted,
    int AttributeUnknown,
    decimal? ConditionFit,
    bool? CityMatched,
    double? SellerRating,
    IReadOnlyList<string> ReasonCodes);

public sealed record SupplierMatchAiDecision(
    string Alias,
    decimal AiScore,
    IReadOnlyList<string> ReasonCodes,
    string ShortExplanation);

public sealed record SupplierMatchAiResult(
    IReadOnlyList<SupplierMatchAiDecision> Rankings);
