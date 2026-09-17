using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.DTOs.Responses.AI;
using HomeCycle.Application.Interfaces.Repositories.AI;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Matching;
using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Pricing.Services;

public sealed class PriceSuggestionService(
    IPriceEvidenceRepository evidenceRepository,
    IProductContextProvider productContextProvider,
    IExternalUsedPriceSearchService externalSearch,
    IPriceSuggestionAiClient aiClient,
    IPriceSuggestionQuota quota,
    DynamicAttributeMatcher attributeMatcher,
    EquivalentModelMatcher equivalentModelMatcher,
    TimeProvider clock) : IPriceSuggestionService
{
    private static readonly HashSet<string> AllowedReasonCodes = new(StringComparer.Ordinal)
    {
        "COMPLETED_TRADE_REFERENCE",
        "INTERNAL_LISTING_REFERENCE",
        "EXTERNAL_USED_LISTING_REFERENCE",
        "EQUIVALENT_MODEL_REFERENCE",
        "NEW_MARKET_PRICE_REFERENCE",
        "LIMITED_EVIDENCE",
        "ATTRIBUTE_MATCH_UNKNOWN",
        "NO_RELIABLE_EVIDENCE"
    };

    public async Task<AiPriceSuggestionResponse?> SuggestAsync(
        Guid userId,
        AiPriceDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productContextProvider.BuildDraftAsync(request, cancellationToken);
        if (product is null)
            return null;

        var now = clock.GetUtcNow();
        var completedCandidates = await evidenceRepository.GetCompletedAsync(
            product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken);

        var evaluatedTrades = completedCandidates
            .Select(item => new EvaluatedTrade(item, attributeMatcher.Match(product.Attributes, item.Attributes)))
            .Where(x => x.Match.Level != AttributeMatchLevel.Conflicted)
            .Take(120)
            .ToList();
        var unknownTradeSamples = evaluatedTrades.Count(x => x.Match.Level == AttributeMatchLevel.Unknown);
        var sameCondition = evaluatedTrades
            .Where(x => x.Evidence.FunctionalityStatus == (int)product.FunctionalityStatus &&
                        x.Evidence.DamageLevel == (int)product.DamageLevel)
            .Select(x => x.Evidence)
            .ToList();
        var trades = sameCondition.Count >= 3
            ? sameCondition
            : evaluatedTrades.Select(x => x.Evidence).ToList();

        var listingCandidates = await evidenceRepository.GetActiveAsync(
            product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken);
        var listings = listingCandidates
            .Where(item => attributeMatcher.Match(product.Attributes, item.Attributes).Level !=
                           AttributeMatchLevel.Conflicted)
            .Take(80)
            .ToList();

        var marketReferences = await evidenceRepository.GetVerifiedMarketAsync(
            product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken);

        var remaining = await quota.RemainingAsync(userId, cancellationToken);
        var reserved = false;
        var external = ExternalUsedPriceSearchResult.Empty;
        if (trades.Count + listings.Count < 2)
        {
            var reservedRemaining = await quota.ReserveAsync(userId, cancellationToken);
            if (reservedRemaining is null)
                return CreateLimitResponse(quota);

            reserved = true;
            remaining = reservedRemaining.Value;
            external = await externalSearch.SearchAsync(product, cancellationToken);
        }

        var exactExternalItems = external.Items
            .Where(x => x.ModelMatchLevel is ExternalModelMatchLevel.Exact or ExternalModelMatchLevel.Variant)
            .ToList();
        var relatedExternalItems = external.Items
            .Where(x => x.ModelMatchLevel == ExternalModelMatchLevel.Related)
            .ToList();

        IReadOnlyList<CompletedPriceEvidence> equivalentTrades = [];
        IReadOnlyList<ListingPriceEvidence> equivalentListings = [];
        var hasExactUsedEvidence = trades.Count > 0 || listings.Count > 0 || exactExternalItems.Count > 0;
        if (hasExactUsedEvidence)
            relatedExternalItems = [];

        if (!hasExactUsedEvidence)
        {
            var equivalentTradeCandidates = await evidenceRepository.GetEquivalentCompletedAsync(
                product.ProductTypeId,
                product.BrandId,
                product.Model,
                now.UtcDateTime,
                cancellationToken);
            equivalentTrades = equivalentTradeCandidates
                .Select(item => (Evidence: item, Match: equivalentModelMatcher.Match(product, item)))
                .Where(x => x.Match.IsAccepted && x.Match.ConditionScore == 1m)
                .OrderByDescending(x => x.Match.OverallScore)
                .ThenByDescending(x => x.Match.ComparedAttributeCount)
                .Select(x => x.Evidence)
                .Take(120)
                .ToList();

            var equivalentListingCandidates = await evidenceRepository.GetEquivalentActiveAsync(
                product.ProductTypeId,
                product.BrandId,
                product.Model,
                now.UtcDateTime,
                cancellationToken);
            equivalentListings = equivalentListingCandidates
                .Select(item => (Evidence: item, Match: equivalentModelMatcher.Match(product, item)))
                .Where(x => x.Match.IsAccepted && x.Match.ConditionScore == 1m)
                .OrderByDescending(x => x.Match.OverallScore)
                .ThenByDescending(x => x.Match.ComparedAttributeCount)
                .Select(x => x.Evidence)
                .Take(80)
                .ToList();
        }

        var completedAll = CalculateStats(trades.Select(x => x.Price));
        var completedRecent = CalculateStats(trades
            .Where(x => x.CompletedAt >= now.AddDays(-30).UtcDateTime)
            .Select(x => x.Price));
        var completedOlder = CalculateStats(trades
            .Where(x => x.CompletedAt < now.AddDays(-30).UtcDateTime)
            .Select(x => x.Price));
        var listingStats = CalculateStats(listings.Select(x => x.Price));
        var externalStats = CalculateStats(exactExternalItems.Select(x => x.PriceVnd));
        var equivalentExternalStats = CalculateStats(relatedExternalItems.Select(x => x.PriceVnd));
        var equivalentCompletedStats = CalculateStats(equivalentTrades.Select(x => x.Price));
        var equivalentListingStats = CalculateStats(equivalentListings.Select(x => x.Price));
        var equivalentCombinedStats = CalculateStats(
            equivalentTrades.Select(x => x.Price)
                .Concat(equivalentListings.Select(x => x.Price))
                .Concat(relatedExternalItems.Select(x => x.PriceVnd)));
        var combinedListingStats = CalculateStats(
            listings.Select(x => x.Price).Concat(exactExternalItems.Select(x => x.PriceVnd)));
        var marketStats = CalculateStats(marketReferences.Select(x => x.PriceVndPerUnit));

        var response = CreateBaseResponse(
            completedAll.SampleCount,
            listingStats.SampleCount,
            externalStats.SampleCount,
            equivalentCompletedStats.SampleCount,
            equivalentListingStats.SampleCount,
            equivalentExternalStats.SampleCount,
            marketStats.SampleCount,
            unknownTradeSamples,
            external.FromCache,
            remaining,
            quota.ResetsAt,
            exactExternalItems.Concat(relatedExternalItems).ToArray(),
            marketReferences);

        var hasExactEvidence = completedAll.SampleCount > 0 ||
                               listingStats.SampleCount > 0 ||
                               externalStats.SampleCount > 0;
        var hasEquivalentEvidence = equivalentCombinedStats.SampleCount > 0;
        var hasReliableEvidence = hasExactEvidence || hasEquivalentEvidence;
        if (!hasReliableEvidence)
        {
            response.Status = "NO_RELIABLE_DATA";
            response.ReasonCodes = ["NO_RELIABLE_EVIDENCE"];
            response.Explanation =
                "Chưa có đủ giao dịch, bài đăng đang hoạt động hoặc tin bán đồ cũ cùng model để đưa ra giá tham khảo.";
            return response;
        }

        if (!reserved)
        {
            var reservedRemaining = await quota.ReserveAsync(userId, cancellationToken);
            if (reservedRemaining is null)
                return CreateLimitResponse(quota);
            response.RemainingToday = reservedRemaining.Value;
        }

        var usingEquivalentEvidence = !hasExactEvidence && hasEquivalentEvidence;
        var basisPrices = usingEquivalentEvidence
            ? equivalentTrades.Select(x => x.Price)
                .Concat(equivalentListings.Select(x => x.Price))
                .Concat(relatedExternalItems.Select(x => x.PriceVnd))
            : listings.Select(x => x.Price)
                .Concat(trades.Select(x => x.Price))
                .Concat(exactExternalItems.Select(x => x.PriceVnd));
        var basis = CalculateStats(basisPrices);
        var allowedRange = new AllowedPriceRange(
            RoundDown(basis.MinPrice!.Value),
            RoundUp(basis.MaxPrice!.Value));

        var context = new PriceSuggestionContext(
            new PriceSuggestionProduct(
                product.ProductTypeName,
                product.BrandName,
                product.Model,
                product.FunctionalityStatus.ToString(),
                product.DamageLevel.ToString(),
                product.UsageDuration,
                product.Attributes.Select(x => new PriceContextAttribute(x.Name, x.DisplayValue, x.Unit)).ToArray()),
            new PriceTimeGroupedStatistics(
                completedRecent,
                completedOlder,
                completedAll,
                sameCondition.Count),
            listingStats,
            externalStats,
            equivalentCompletedStats,
            equivalentListingStats,
            equivalentExternalStats,
            marketStats,
            allowedRange,
            new PriceEvidenceQuality(
                evaluatedTrades.Count(x => x.Match.Level == AttributeMatchLevel.Matched),
                unknownTradeSamples,
                exactExternalItems.Count > 0 || relatedExternalItems.Count > 0,
                external.FromCache));

        var decision = await aiClient.SuggestAsync(context, cancellationToken);
        if (TryAcceptDecision(decision, context, out var accepted))
        {
            response.Status = "SUGGESTED";
            response.SuggestedPrice = accepted.SuggestedPrice;
            response.MinPrice = accepted.MinPrice;
            response.MaxPrice = accepted.MaxPrice;
            response.ReasonCodes = accepted.ReasonCodes.ToList();
            response.Explanation = accepted.ShortExplanation;
            response.Confidence = GetConfidence(
                completedAll.SampleCount,
                listingStats.SampleCount,
                externalStats.SampleCount,
                usingEquivalentEvidence);
            return response;
        }

        ApplyFallback(
            response,
            completedAll,
            listingStats,
            externalStats,
            combinedListingStats,
            equivalentCombinedStats,
            usingEquivalentEvidence);
        return response;
    }

    private static AiPriceSuggestionResponse CreateBaseResponse(
        int completedTrades,
        int internalListings,
        int externalListings,
        int equivalentCompletedTrades,
        int equivalentInternalListings,
        int equivalentExternalListings,
        int newMarketPrices,
        int unknownAttributeSamples,
        bool externalSearchFromCache,
        int remainingToday,
        DateTimeOffset resetsAt,
        IReadOnlyList<ExternalUsedPriceEvidence> externalSources,
        IReadOnlyList<MarketPriceEvidence> marketSources)
    {
        return new AiPriceSuggestionResponse
        {
            RemainingToday = remainingToday,
            ResetsAt = resetsAt,
            Evidence = new AiPriceEvidenceSummary
            {
                CompletedTradeSamples = completedTrades,
                InternalListingSamples = internalListings,
                ExternalListingSamples = externalListings,
                EquivalentCompletedTradeSamples = equivalentCompletedTrades,
                EquivalentInternalListingSamples = equivalentInternalListings,
                EquivalentExternalListingSamples = equivalentExternalListings,
                NewMarketPriceSamples = newMarketPrices,
                UnknownAttributeSamples = unknownAttributeSamples,
                ExternalSearchFromCache = externalSearchFromCache
            },
            Sources = externalSources.Select(x => new AiPriceSource(
                    x.ModelMatchLevel == ExternalModelMatchLevel.Related
                        ? "EXTERNAL_EQUIVALENT_LISTING"
                        : "EXTERNAL_USED_LISTING",
                    x.SourceName,
                    x.SourceUrl,
                    x.RetrievedAt))
                .Concat(marketSources.Take(3).Select(x => new AiPriceSource(
                    "NEW_MARKET_REFERENCE", x.SourceName, x.SourceUrl, AsUtcOffset(x.ObservedAt))))
                .ToList()
        };
    }

    private static AiPriceSuggestionResponse CreateLimitResponse(IPriceSuggestionQuota quota) => new()
    {
        Status = "DAILY_LIMIT_REACHED",
        Explanation = $"Bạn đã dùng hết {quota.DailyLimit} lượt gợi ý giá hôm nay.",
        RemainingToday = 0,
        ResetsAt = quota.ResetsAt
    };

    private static void ApplyFallback(
        AiPriceSuggestionResponse response,
        PriceStatistics completed,
        PriceStatistics internalListings,
        PriceStatistics external,
        PriceStatistics combinedListings,
        PriceStatistics equivalentCombined,
        bool usingEquivalentEvidence)
    {
        if (usingEquivalentEvidence && equivalentCombined.SampleCount > 0)
        {
            response.Status = "FALLBACK_EQUIVALENT_MODEL";
            response.SuggestedPrice = RoundNearest(equivalentCombined.MedianPrice!.Value);
            response.MinPrice = RoundDown(equivalentCombined.MinPrice!.Value);
            response.MaxPrice = RoundUp(equivalentCombined.MaxPrice!.Value);
            response.Confidence = "LOW";
            response.ReasonCodes = ["EQUIVALENT_MODEL_REFERENCE", "LIMITED_EVIDENCE"];
            response.Explanation =
                "AI chưa trả được kết quả hợp lệ. Đây là giá tham khảo từ các model cùng hãng, cùng loại và có thuộc tính phù hợp.";
            return;
        }

        if (completed.SampleCount > 0)
        {
            response.Status = "FALLBACK_DB_ONLY";
            response.SuggestedPrice = RoundNearest(completed.MedianPrice!.Value);
            response.MinPrice = RoundDown(completed.MinPrice!.Value);
            response.MaxPrice = RoundUp(completed.MaxPrice!.Value);
            response.Confidence = completed.SampleCount >= 3 ? "MEDIUM" : "LOW";
            response.ReasonCodes = ["COMPLETED_TRADE_REFERENCE", "LIMITED_EVIDENCE"];
            response.Explanation = "AI chưa trả được kết quả hợp lệ. Đây là giá tham khảo từ giao dịch cùng model trên HomeCycle.";
            return;
        }

        if (internalListings.SampleCount >= 1 && external.SampleCount >= 1)
        {
            response.Status = "FALLBACK_MARKET_LISTINGS";
            response.SuggestedPrice = RoundNearest(combinedListings.MedianPrice!.Value);
            response.MinPrice = RoundDown(combinedListings.MinPrice!.Value);
            response.MaxPrice = RoundUp(combinedListings.MaxPrice!.Value);
            response.Confidence = "LOW";
            response.ReasonCodes =
                ["INTERNAL_LISTING_REFERENCE", "EXTERNAL_USED_LISTING_REFERENCE", "LIMITED_EVIDENCE"];
            response.Explanation =
                "AI chưa trả được kết quả hợp lệ. Đây là giá tham khảo tổng hợp từ bài đăng HomeCycle và tin bán đồ cũ cùng model.";
            return;
        }

        if (internalListings.SampleCount >= 1)
        {
            response.Status = "FALLBACK_INTERNAL_LISTINGS";
            response.SuggestedPrice = RoundNearest(internalListings.MedianPrice!.Value);
            response.MinPrice = RoundDown(internalListings.MinPrice!.Value);
            response.MaxPrice = RoundUp(internalListings.MaxPrice!.Value);
            response.Confidence = internalListings.SampleCount >= 3 ? "MEDIUM" : "LOW";
            response.ReasonCodes = ["INTERNAL_LISTING_REFERENCE", "LIMITED_EVIDENCE"];
            response.Explanation =
                "AI chưa trả được kết quả hợp lệ. Đây là giá tham khảo từ các bài đăng đang hoạt động cùng model trên HomeCycle.";
            return;
        }

        if (external.SampleCount >= 1)
        {
            response.Status = "FALLBACK_MARKET_LISTINGS";
            response.SuggestedPrice = RoundNearest(external.MedianPrice!.Value);
            response.MinPrice = RoundDown(external.MinPrice!.Value);
            response.MaxPrice = RoundUp(external.MaxPrice!.Value);
            response.Confidence = "LOW";
            response.ReasonCodes = ["EXTERNAL_USED_LISTING_REFERENCE", "LIMITED_EVIDENCE"];
            response.Explanation = "AI chưa trả được kết quả hợp lệ. Đây là giá tham khảo từ các tin bán đồ cũ cùng model.";
            return;
        }

        response.Status = "NO_RELIABLE_DATA";
        response.Confidence = "NONE";
        response.ReasonCodes = ["NO_RELIABLE_EVIDENCE"];
        response.Explanation = "Không còn chứng cứ giá hợp lệ để tạo giá tham khảo.";
    }

    private static bool TryAcceptDecision(
        AiPriceDecision? decision,
        PriceSuggestionContext context,
        out AiPriceDecision accepted)
    {
        accepted = null!;
        if (decision?.SuggestedPrice is not > 0 || decision.MinPrice is not > 0 ||
            decision.MaxPrice is not > 0 || decision.ReasonCodes.Count == 0 ||
            decision.ReasonCodes.Any(code => !AllowedReasonCodes.Contains(code)) ||
            decision.ReasonCodes.Contains("NO_RELIABLE_EVIDENCE", StringComparer.Ordinal) ||
            !ReasonCodesMatchEvidence(decision.ReasonCodes, context) ||
            string.IsNullOrWhiteSpace(decision.ShortExplanation) ||
            decision.ShortExplanation.Length > 300 || CountSentences(decision.ShortExplanation) > 2)
            return false;

        var suggested = RoundNearest(decision.SuggestedPrice.Value);
        var min = RoundNearest(decision.MinPrice.Value);
        var max = RoundNearest(decision.MaxPrice.Value);
        if (min > suggested || suggested > max ||
            min < context.AllowedPriceRange.MinPrice || max > context.AllowedPriceRange.MaxPrice)
            return false;

        accepted = decision with
        {
            SuggestedPrice = suggested,
            MinPrice = min,
            MaxPrice = max,
            ShortExplanation = decision.ShortExplanation.Trim()
        };
        return true;
    }

    private static bool ReasonCodesMatchEvidence(
        IReadOnlyList<string> reasonCodes,
        PriceSuggestionContext context)
    {
        var hasExactUsedEvidence = context.CompletedTrades.All.SampleCount > 0 ||
                                   context.ActiveListings.SampleCount > 0 ||
                                   context.ExternalUsedListings.SampleCount > 0;
        var hasEquivalentEvidence = context.EquivalentCompletedTrades.SampleCount > 0 ||
                                    context.EquivalentActiveListings.SampleCount > 0 ||
                                    context.EquivalentExternalUsedListings.SampleCount > 0;

        if (reasonCodes.Contains("COMPLETED_TRADE_REFERENCE", StringComparer.Ordinal) &&
            context.CompletedTrades.All.SampleCount == 0)
            return false;
        if (reasonCodes.Contains("INTERNAL_LISTING_REFERENCE", StringComparer.Ordinal) &&
            context.ActiveListings.SampleCount == 0)
            return false;
        if (reasonCodes.Contains("EXTERNAL_USED_LISTING_REFERENCE", StringComparer.Ordinal) &&
            context.ExternalUsedListings.SampleCount == 0 &&
            context.EquivalentExternalUsedListings.SampleCount == 0)
            return false;
        if (reasonCodes.Contains("EQUIVALENT_MODEL_REFERENCE", StringComparer.Ordinal) &&
            !hasEquivalentEvidence)
            return false;
        if (!hasExactUsedEvidence && hasEquivalentEvidence &&
            !reasonCodes.Contains("EQUIVALENT_MODEL_REFERENCE", StringComparer.Ordinal))
            return false;
        if (reasonCodes.Contains("NEW_MARKET_PRICE_REFERENCE", StringComparer.Ordinal) &&
            context.NewMarketPrices.SampleCount == 0)
            return false;
        if (reasonCodes.Contains("ATTRIBUTE_MATCH_UNKNOWN", StringComparer.Ordinal) &&
            context.EvidenceQuality.UnknownAttributeSamples == 0)
            return false;
        return true;
    }

    private static PriceStatistics CalculateStats(IEnumerable<decimal> source)
    {
        var values = source.Where(x => x > 0).OrderBy(x => x).ToArray();
        if (values.Length == 0)
            return new PriceStatistics(0, null, null, null);

        if (values.Length >= 4)
        {
            var half = values.Length / 2;
            var lowerHalf = values.Take(half).ToArray();
            var upperHalf = values.Skip((values.Length + 1) / 2).ToArray();
            var firstQuartile = CalculateMedian(lowerHalf);
            var thirdQuartile = CalculateMedian(upperHalf);
            var interquartileRange = thirdQuartile - firstQuartile;
            var lowerBound = firstQuartile - 1.5m * interquartileRange;
            var upperBound = thirdQuartile + 1.5m * interquartileRange;
            var filtered = values
                .Where(value => value >= lowerBound && value <= upperBound)
                .ToArray();
            if (filtered.Length > 0)
                values = filtered;
        }

        var median = CalculateMedian(values);
        return new PriceStatistics(values.Length, median, values[0], values[^1]);
    }

    private static decimal CalculateMedian(IReadOnlyList<decimal> sortedValues)
    {
        var midpoint = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[midpoint - 1] + sortedValues[midpoint]) / 2
            : sortedValues[midpoint];
    }

    private static string GetConfidence(
        int completedSamples,
        int internalSamples,
        int externalSamples,
        bool usingEquivalentEvidence) =>
        usingEquivalentEvidence
            ? "LOW"
            : completedSamples >= 3
            ? "HIGH"
            : completedSamples >= 1 || internalSamples >= 3
                ? "MEDIUM"
                : internalSamples + externalSamples >= 1
                    ? "LOW"
                    : "NONE";

    private static int CountSentences(string value) =>
        value.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static decimal RoundNearest(decimal value) =>
        Math.Round(value / 10_000m, 0, MidpointRounding.AwayFromZero) * 10_000m;

    private static decimal RoundDown(decimal value) => Math.Floor(value / 10_000m) * 10_000m;
    private static decimal RoundUp(decimal value) => Math.Ceiling(value / 10_000m) * 10_000m;

    private static DateTimeOffset AsUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record EvaluatedTrade(CompletedPriceEvidence Evidence, AttributeMatchResult Match);
}
