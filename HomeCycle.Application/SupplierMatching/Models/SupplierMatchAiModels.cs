namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchAiCandidate(
    string Alias,
    decimal BackendScore,
    string MatchLevel,
    string ModelMatchLevel,
    decimal? Price,
    int AvailableQuantity,
    bool CanFulfillQuantity,
    double? SellerRating,
    IReadOnlyList<string> MatchedCriteria,
    IReadOnlyList<string> UnmatchedCriteria,
    IReadOnlyList<string> UnknownCriteria,
    IReadOnlyList<string> ReasonCodes);

public sealed record SupplierMatchAiDecision(
    string Alias,
    decimal AiScore,
    IReadOnlyList<string> ReasonCodes,
    string ShortExplanation);

public sealed record SupplierMatchAiResult(
    IReadOnlyList<SupplierMatchAiDecision> Rankings);
