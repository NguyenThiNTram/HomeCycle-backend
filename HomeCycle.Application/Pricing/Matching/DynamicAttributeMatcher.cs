using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Pricing.Matching;

public enum AttributeMatchLevel
{
    Unknown = 0,
    Matched = 1,
    Conflicted = 2
}

public sealed record AttributeMatchResult(
    AttributeMatchLevel Level,
    int ComparedCount,
    int MatchedCount,
    IReadOnlyList<Guid> ConflictedAttributeIds);

public sealed class DynamicAttributeMatcher
{
    public AttributeMatchResult Match(
        IReadOnlyCollection<DynamicAttributeValue> requested,
        IReadOnlyCollection<DynamicAttributeValue> evidence)
    {
        var evidenceById = evidence
            .GroupBy(x => x.AttributeId)
            .ToDictionary(x => x.Key, x => x.First());

        var compared = 0;
        var matched = 0;
        var conflicts = new List<Guid>();

        foreach (var requestedValue in requested)
        {
            if (!evidenceById.TryGetValue(requestedValue.AttributeId, out var evidenceValue))
                continue;

            var requestedNormalized = AttributeValueNormalizer.Normalize(requestedValue);
            var evidenceNormalized = AttributeValueNormalizer.Normalize(evidenceValue);
            if (requestedNormalized is null || evidenceNormalized is null)
                continue;

            compared++;
            if (StringComparer.Ordinal.Equals(requestedNormalized, evidenceNormalized))
                matched++;
            else
                conflicts.Add(requestedValue.AttributeId);
        }

        var level = conflicts.Count > 0
            ? AttributeMatchLevel.Conflicted
            : compared > 0
                ? AttributeMatchLevel.Matched
                : AttributeMatchLevel.Unknown;

        return new AttributeMatchResult(level, compared, matched, conflicts);
    }
}
