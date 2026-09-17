using System.Globalization;
using System.Text.RegularExpressions;
using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.SupplierMatching.Scoring;

public sealed partial class SupplierMatchScorer
{
    public SupplierMatchEvaluation Evaluate(SupplierDemandContext demand, SupplierCandidate candidate)
    {
        var matched = new List<string>();
        var unmatched = new List<string>();
        var unknown = new List<string>();
        var reasons = new List<string>();
        var attributeStates = new Dictionary<Guid, SupplierCriterionState>();
        decimal earned = 0;
        decimal applicable = 0;

        var classification = ScoreClassification(demand, candidate, matched, unmatched, ref earned, ref applicable);
        if (classification < 0)
            return Disqualified(attributeStates, matched, unmatched, unknown);

        var advancedFilterFailure = ValidateAdvancedFilters(demand, candidate);
        if (advancedFilterFailure is not null)
            return Disqualified(attributeStates, matched, [.. unmatched, advancedFilterFailure], unknown, advancedFilterFailure);

        var attributeScore = ScoreAttributes(
            demand, candidate, attributeStates, matched, unmatched, unknown, ref earned, ref applicable);
        var brandAndModel = ScoreBrandAndModel(
            demand, candidate, matched, unmatched, unknown, reasons, ref earned, ref applicable,
            out var modelLevel);
        var budget = ScoreBudget(demand, candidate, matched, unmatched, unknown, reasons, ref earned, ref applicable);
        var quantity = ScoreQuantity(demand, candidate, matched, unmatched, reasons, ref earned, ref applicable);
        var condition = ScoreCondition(demand, candidate, matched, unmatched, unknown, reasons, ref earned, ref applicable);
        var city = ScoreCity(demand, candidate, matched, unmatched, unknown, ref earned, ref applicable);
        var rating = ScoreRating(candidate, matched, unknown, reasons, ref earned, ref applicable);

        var baseScore = applicable <= 0 ? 0 : Math.Round(earned / applicable * 10m, 2);
        var level = baseScore >= 8m
            ? SupplierMatchLevel.High
            : baseScore >= 6m
                ? SupplierMatchLevel.Medium
                : SupplierMatchLevel.Low;

        if (attributeStates.Values.Any(x => x == SupplierCriterionState.Unknown))
            reasons.Add("ATTRIBUTE_MATCH_UNKNOWN");
        if (modelLevel == SupplierModelMatchLevel.Exact)
            reasons.Add("EXACT_MODEL");
        else if (modelLevel == SupplierModelMatchLevel.Variant)
            reasons.Add("MODEL_VARIANT");
        else if (modelLevel == SupplierModelMatchLevel.Related)
            reasons.Add("RELATED_MODEL");

        return new SupplierMatchEvaluation(
            baseScore,
            level,
            modelLevel,
            new SupplierScoreBreakdown(
                classification,
                attributeScore,
                brandAndModel,
                budget,
                quantity,
                condition,
                city,
                rating,
                earned,
                applicable),
            matched.Distinct(StringComparer.Ordinal).ToArray(),
            unmatched.Distinct(StringComparer.Ordinal).ToArray(),
            unknown.Distinct(StringComparer.Ordinal).ToArray(),
            reasons.Distinct(StringComparer.Ordinal).ToArray(),
            attributeStates,
            candidate.AvailableQuantity >= demand.Quantity,
            false);
    }

    private static decimal ScoreClassification(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ref decimal earned,
        ref decimal applicable)
    {
        const decimal weight = 2m;
        applicable += weight;
        var matches = demand.ProductTypeId.HasValue
            ? candidate.ProductTypeId == demand.ProductTypeId
            : demand.CategoryId.HasValue && candidate.CategoryId == demand.CategoryId;
        if (!matches)
        {
            unmatched.Add("PRODUCT_CLASSIFICATION");
            return -1m;
        }

        earned += weight;
        matched.Add("PRODUCT_CLASSIFICATION");
        return weight;
    }

    private static decimal ScoreAttributes(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        IDictionary<Guid, SupplierCriterionState> states,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> unknown,
        ref decimal earned,
        ref decimal applicable)
    {
        if (demand.Attributes.Count == 0)
            return 0;

        const decimal weight = 2m;
        applicable += weight;
        var matchedCount = 0;
        foreach (var expected in demand.Attributes)
        {
            var actual = candidate.Attributes.FirstOrDefault(x => x.AttributeId == expected.AttributeId);
            if (actual is null)
            {
                states[expected.AttributeId] = SupplierCriterionState.Unknown;
                unknown.Add($"ATTRIBUTE:{expected.AttributeId:D}");
                continue;
            }

            if (AttributeValuesEqual(expected, actual))
            {
                states[expected.AttributeId] = SupplierCriterionState.Matched;
                matched.Add($"ATTRIBUTE:{expected.AttributeId:D}");
                matchedCount++;
            }
            else
            {
                states[expected.AttributeId] = SupplierCriterionState.Conflicted;
                unmatched.Add($"ATTRIBUTE:{expected.AttributeId:D}");
            }
        }

        var score = weight * matchedCount / demand.Attributes.Count;
        earned += score;
        return score;
    }

    private static decimal ScoreBrandAndModel(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> unknown,
        ICollection<string> reasons,
        ref decimal earned,
        ref decimal applicable,
        out SupplierModelMatchLevel modelLevel)
    {
        decimal score = 0;
        modelLevel = SupplierModelMatchLevel.NotSpecified;

        if (demand.BrandId.HasValue)
        {
            const decimal brandWeight = 0.5m;
            applicable += brandWeight;
            if (!candidate.BrandId.HasValue)
                unknown.Add("BRAND");
            else if (candidate.BrandId == demand.BrandId)
            {
                score += brandWeight;
                matched.Add("BRAND");
                reasons.Add("BRAND_MATCH");
            }
            else
                unmatched.Add("BRAND");
        }

        if (!string.IsNullOrWhiteSpace(demand.NormalizedModelNumber))
        {
            const decimal modelWeight = 1m;
            applicable += modelWeight;
            var (level, ratio) = CompareModels(
                demand.NormalizedModelNumber!, candidate.NormalizedModelNumber);
            modelLevel = level;
            if (level == SupplierModelMatchLevel.Unknown)
                unknown.Add("MODEL");
            else if (level == SupplierModelMatchLevel.Unrelated)
                unmatched.Add("MODEL");
            else
            {
                score += modelWeight * ratio;
                matched.Add("MODEL");
            }
        }

        earned += score;
        return score;
    }

    private static decimal ScoreBudget(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> unknown,
        ICollection<string> reasons,
        ref decimal earned,
        ref decimal applicable)
    {
        if (!demand.PriceFrom.HasValue && !demand.PriceTo.HasValue)
            return 0;

        const decimal weight = 1.5m;
        applicable += weight;
        if (candidate.Price is not > 0)
        {
            unknown.Add("PRICE");
            return 0;
        }

        decimal ratio;
        if (demand.PriceFrom.HasValue && candidate.Price < demand.PriceFrom)
            ratio = demand.PriceFrom.Value <= 0 ? 1m : candidate.Price.Value / demand.PriceFrom.Value;
        else if (demand.PriceTo.HasValue && candidate.Price > demand.PriceTo)
            ratio = candidate.Price.Value <= 0 ? 0m : demand.PriceTo.Value / candidate.Price.Value;
        else
            ratio = 1m;

        ratio = Math.Clamp(ratio, 0m, 1m);
        var score = weight * ratio;
        earned += score;
        if (ratio == 1m)
        {
            matched.Add("PRICE");
            reasons.Add("WITHIN_BUDGET");
        }
        else
            unmatched.Add("PRICE");
        return score;
    }

    private static decimal ScoreQuantity(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> reasons,
        ref decimal earned,
        ref decimal applicable)
    {
        const decimal weight = 1m;
        applicable += weight;
        var ratio = Math.Clamp((decimal)candidate.AvailableQuantity / Math.Max(1, demand.Quantity), 0m, 1m);
        var score = weight * ratio;
        earned += score;
        if (ratio == 1m)
        {
            matched.Add("QUANTITY");
            reasons.Add("FULL_QUANTITY_AVAILABLE");
        }
        else
        {
            unmatched.Add("QUANTITY");
            if (candidate.AvailableQuantity > 0)
                reasons.Add("PARTIAL_QUANTITY_AVAILABLE");
        }
        return score;
    }

    private static decimal ScoreCondition(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> unknown,
        ICollection<string> reasons,
        ref decimal earned,
        ref decimal applicable)
    {
        decimal score = 0;
        if (demand.FunctionalityStatus.HasValue)
        {
            const decimal weight = 0.4m;
            applicable += weight;
            if (!candidate.FunctionalityStatus.HasValue)
                unknown.Add("FUNCTIONALITY");
            else if ((int)candidate.FunctionalityStatus.Value <= (int)demand.FunctionalityStatus.Value)
            {
                score += weight;
                matched.Add("FUNCTIONALITY");
            }
            else
                unmatched.Add("FUNCTIONALITY");
        }

        if (demand.DamageLevel.HasValue)
        {
            const decimal weight = 0.4m;
            applicable += weight;
            if (!candidate.DamageLevel.HasValue)
                unknown.Add("DAMAGE");
            else if ((int)candidate.DamageLevel.Value <= (int)demand.DamageLevel.Value)
            {
                score += weight;
                matched.Add("DAMAGE");
            }
            else
                unmatched.Add("DAMAGE");
        }

        if (demand.UsageDuration.HasValue)
        {
            const decimal weight = 0.2m;
            applicable += weight;
            if (!candidate.UsageDuration.HasValue)
                unknown.Add("USAGE_DURATION");
            else
            {
                var ratio = candidate.UsageDuration.Value <= demand.UsageDuration.Value
                    ? 1m
                    : demand.UsageDuration.Value <= 0
                        ? 0m
                        : (decimal)demand.UsageDuration.Value / candidate.UsageDuration.Value;
                score += weight * Math.Clamp(ratio, 0m, 1m);
                if (ratio == 1m)
                    matched.Add("USAGE_DURATION");
                else
                    unmatched.Add("USAGE_DURATION");
            }
        }

        earned += score;
        if (score > 0)
            reasons.Add("CONDITION_COMPATIBLE");
        return score;
    }

    private static decimal ScoreCity(
        SupplierDemandContext demand,
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unmatched,
        ICollection<string> unknown,
        ref decimal earned,
        ref decimal applicable)
    {
        if (string.IsNullOrWhiteSpace(demand.City))
            return 0;

        const decimal weight = 0.5m;
        applicable += weight;
        if (string.IsNullOrWhiteSpace(candidate.City))
        {
            unknown.Add("CITY");
            return 0;
        }

        if (!string.Equals(demand.City, candidate.City, StringComparison.OrdinalIgnoreCase))
        {
            unmatched.Add("CITY");
            return 0;
        }

        earned += weight;
        matched.Add("CITY");
        return weight;
    }

    private static decimal ScoreRating(
        SupplierCandidate candidate,
        ICollection<string> matched,
        ICollection<string> unknown,
        ICollection<string> reasons,
        ref decimal earned,
        ref decimal applicable)
    {
        const decimal weight = 0.5m;
        applicable += weight;
        if (!candidate.AverageRating.HasValue || candidate.TotalReviews == 0)
        {
            unknown.Add("SUPPLIER_RATING");
            return 0;
        }

        var ratio = Math.Clamp((decimal)candidate.AverageRating.Value / 5m, 0m, 1m);
        var score = weight * ratio;
        earned += score;
        matched.Add("SUPPLIER_RATING");
        if (candidate.AverageRating >= 4)
            reasons.Add("REPUTABLE_SUPPLIER");
        return score;
    }

    private static bool AttributeValuesEqual(
        SupplierCandidateAttribute expected,
        SupplierCandidateAttribute actual)
    {
        if (expected.OptionId.HasValue || actual.OptionId.HasValue)
            return expected.OptionId.HasValue && expected.OptionId == actual.OptionId;
        if (expected.NumberValue.HasValue || actual.NumberValue.HasValue)
            return expected.NumberValue.HasValue && expected.NumberValue == actual.NumberValue;
        if (expected.BooleanValue.HasValue || actual.BooleanValue.HasValue)
            return expected.BooleanValue.HasValue && expected.BooleanValue == actual.BooleanValue;
        if (!string.IsNullOrWhiteSpace(expected.TextValue) || !string.IsNullOrWhiteSpace(actual.TextValue))
            return NormalizeText(expected.TextValue) == NormalizeText(actual.TextValue);
        return false;
    }

    private static (SupplierModelMatchLevel Level, decimal Ratio) CompareModels(
        string expected,
        string? actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return (SupplierModelMatchLevel.Unknown, 0m);
        if (StringComparer.Ordinal.Equals(expected, actual))
            return (SupplierModelMatchLevel.Exact, 1m);
        if (IsRegionalVariant(expected, actual))
            return (SupplierModelMatchLevel.Variant, 0.9m);

        var similarity = CalculateSimilarity(expected, actual);
        return similarity >= 0.45m
            ? (SupplierModelMatchLevel.Related, similarity)
            : (SupplierModelMatchLevel.Unrelated, 0m);
    }

    private static bool IsRegionalVariant(string left, string right)
    {
        var shorter = left.Length <= right.Length ? left : right;
        var longer = left.Length > right.Length ? left : right;
        if (!longer.StartsWith(shorter, StringComparison.Ordinal))
            return false;
        var suffix = longer[shorter.Length..];
        return suffix.Length is >= 1 and <= 3 && suffix.All(char.IsAsciiLetter);
    }

    private static decimal CalculateSimilarity(string left, string right)
    {
        var maxLength = Math.Max(left.Length, right.Length);
        if (maxLength == 0)
            return 1m;

        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];
        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitution = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitution);
            }
            (previous, current) = (current, previous);
        }

        return Math.Round(Math.Max(0m, 1m - (decimal)previous[right.Length] / maxLength), 3);
    }

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Whitespace().Replace(value.Trim().ToLowerInvariant(), " ");

    private static SupplierMatchEvaluation Disqualified(
        IReadOnlyDictionary<Guid, SupplierCriterionState> attributeStates,
        IReadOnlyList<string> matched,
        IReadOnlyList<string> unmatched,
        IReadOnlyList<string> unknown,
        string reasonCode = "PRODUCT_CLASSIFICATION_MISMATCH") => new(
            0,
            SupplierMatchLevel.Low,
            SupplierModelMatchLevel.NotSpecified,
            new SupplierScoreBreakdown(0, 0, 0, 0, 0, 0, 0, 0, 0, 2),
            matched,
            unmatched,
            unknown,
            [reasonCode],
            attributeStates,
            false,
            true);

    private static string? ValidateAdvancedFilters(
        SupplierDemandContext demand,
        SupplierCandidate candidate)
    {
        var filters = demand.AdvancedFilters;
        if (filters.RequireFullQuantity && candidate.AvailableQuantity < demand.Quantity)
            return "ADVANCED_QUANTITY_FILTER_MISMATCH";

        if (filters.StrictBrand && demand.BrandId.HasValue && candidate.BrandId != demand.BrandId)
            return "ADVANCED_BRAND_FILTER_MISMATCH";

        if (filters.StrictBudget &&
            (candidate.Price is not > 0 ||
             demand.PriceFrom.HasValue && candidate.Price < demand.PriceFrom ||
             demand.PriceTo.HasValue && candidate.Price > demand.PriceTo))
            return "ADVANCED_BUDGET_FILTER_MISMATCH";

        if (filters.SameCityOnly &&
            !string.IsNullOrWhiteSpace(demand.City) &&
            !string.Equals(demand.City, candidate.City, StringComparison.OrdinalIgnoreCase))
            return "ADVANCED_CITY_FILTER_MISMATCH";

        if (filters.MinimumSellerRating.HasValue &&
            (!candidate.AverageRating.HasValue || candidate.AverageRating < filters.MinimumSellerRating))
            return "ADVANCED_RATING_FILTER_MISMATCH";

        return null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
