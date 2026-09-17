namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchEvaluation(
    decimal BaseScore,
    SupplierMatchLevel MatchLevel,
    SupplierModelMatchLevel ModelMatchLevel,
    SupplierScoreBreakdown Breakdown,
    IReadOnlyList<string> MatchedCriteria,
    IReadOnlyList<string> UnmatchedCriteria,
    IReadOnlyList<string> UnknownCriteria,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyDictionary<Guid, SupplierCriterionState> AttributeStates,
    bool CanFulfillQuantity,
    bool IsDisqualified);
