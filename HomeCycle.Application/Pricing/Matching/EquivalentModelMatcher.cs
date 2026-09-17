using HomeCycle.Application.Interfaces.Repositories.AI;
using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Pricing.Matching;

public sealed record EquivalentModelMatchResult(
    bool IsAccepted,
    decimal AttributeScore,
    decimal ConditionScore,
    decimal OverallScore,
    int ComparedAttributeCount,
    int RequiredAttributeCount,
    IReadOnlyList<Guid> ConflictedAttributeIds);

public sealed class EquivalentModelMatcher(DynamicAttributeMatcher attributeMatcher)
{
    public EquivalentModelMatchResult Match(
        DynamicProductContext requested,
        CompletedPriceEvidence evidence) =>
        MatchCore(
            requested,
            evidence.NormalizedModelNumber,
            evidence.FunctionalityStatus,
            evidence.DamageLevel,
            evidence.Attributes);

    public EquivalentModelMatchResult Match(
        DynamicProductContext requested,
        ListingPriceEvidence evidence) =>
        MatchCore(
            requested,
            evidence.NormalizedModelNumber,
            evidence.FunctionalityStatus,
            evidence.DamageLevel,
            evidence.Attributes);

    private EquivalentModelMatchResult MatchCore(
        DynamicProductContext requested,
        string candidateModel,
        int? functionalityStatus,
        int? damageLevel,
        IReadOnlyCollection<DynamicAttributeValue> candidateAttributes)
    {
        // Product type and brand equality are enforced by IPriceEvidenceRepository queries.
        if (string.IsNullOrWhiteSpace(candidateModel) ||
            StringComparer.Ordinal.Equals(requested.Model, candidateModel))
            return Rejected();

        var comparableRequestedCount = requested.Attributes.Count(
            attribute => AttributeValueNormalizer.Normalize(attribute) is not null);
        if (comparableRequestedCount == 0)
            return Rejected();

        var requiredAttributeCount = Math.Min(2, comparableRequestedCount);
        var attributeMatch = attributeMatcher.Match(requested.Attributes, candidateAttributes);
        var attributeScore = attributeMatch.ComparedCount == 0
            ? 0m
            : (decimal)attributeMatch.MatchedCount / attributeMatch.ComparedCount;
        var conditionScore = functionalityStatus == (int)requested.FunctionalityStatus &&
                             damageLevel == (int)requested.DamageLevel
            ? 1m
            : 0m;
        var isAccepted = attributeMatch.Level == AttributeMatchLevel.Matched &&
                         attributeMatch.ComparedCount >= requiredAttributeCount &&
                         attributeScore == 1m;
        var overallScore = isAccepted
            ? attributeScore * 0.8m + conditionScore * 0.2m
            : 0m;

        return new EquivalentModelMatchResult(
            isAccepted,
            attributeScore,
            conditionScore,
            overallScore,
            attributeMatch.ComparedCount,
            requiredAttributeCount,
            attributeMatch.ConflictedAttributeIds);
    }

    private static EquivalentModelMatchResult Rejected() =>
        new(false, 0m, 0m, 0m, 0, 0, []);
}
