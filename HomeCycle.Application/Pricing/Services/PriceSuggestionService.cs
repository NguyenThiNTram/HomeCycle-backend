using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.DTOs.Responses.AI;
using HomeCycle.Application.Interfaces.Repositories.AI;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Matching;
using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Pricing.Services;

// Giá gợi ý = giá gốc (máy tình trạng tốt) × hệ số tình trạng, tối đa 85% giá bán mới.
// Giá gốc lấy từ giá máy cũ cùng model (đã quy về tình trạng tốt); thiếu mẫu thì kết hợp
// hoặc thay bằng giá bán mới đã khấu hao theo thời gian sử dụng. Gemini chỉ dùng để tìm dữ liệu.
public sealed class PriceSuggestionService(
    IPriceEvidenceRepository evidenceRepository,
    IProductContextProvider productContextProvider,
    IExternalUsedPriceSearchService externalSearch,
    INewPriceSearchService newPriceSearch,
    IPriceSuggestionQuota quota,
    DynamicAttributeMatcher attributeMatcher,
    EquivalentModelMatcher equivalentModelMatcher,
    TimeProvider clock) : IPriceSuggestionService
{
    private const int ReliableUsedSampleCount = 3;

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
        var trades = evaluatedTrades.Select(x => x.Evidence).ToList();

        var listingCandidates = await evidenceRepository.GetActiveAsync(
            product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken);
        var listings = listingCandidates
            .Where(item => attributeMatcher.Match(product.Attributes, item.Attributes).Level !=
                           AttributeMatchLevel.Conflicted)
            .Take(80)
            .ToList();

        var marketReferences = await evidenceRepository.GetVerifiedMarketAsync(
            product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken);

        var dailyLimit = await quota.GetDailyLimitAsync(userId, cancellationToken);
        var remaining = await quota.RemainingAsync(userId, dailyLimit, cancellationToken);
        var reserved = false;
        var external = ExternalUsedPriceSearchResult.Empty;
        var searchedNewPrices = NewPriceSearchResult.Empty;
        var needsExternalUsed = trades.Count + listings.Count < 2;
        var needsNewPriceSearch = marketReferences.Count == 0;
        if (needsExternalUsed || needsNewPriceSearch)
        {
            var reservedRemaining = await quota.ReserveAsync(userId, dailyLimit, cancellationToken);
            if (reservedRemaining is null)
                return CreateLimitResponse(quota, dailyLimit);

            reserved = true;
            remaining = reservedRemaining.Value;

            // Hai lượt tìm độc lập, chạy song song để không vượt thời gian chờ của app.
            var externalTask = needsExternalUsed
                ? externalSearch.SearchAsync(product, cancellationToken)
                : Task.FromResult(ExternalUsedPriceSearchResult.Empty);
            var newPriceTask = needsNewPriceSearch
                ? newPriceSearch.SearchAsync(product, cancellationToken)
                : Task.FromResult(NewPriceSearchResult.Empty);
            await Task.WhenAll(externalTask, newPriceTask);
            external = await externalTask;
            searchedNewPrices = await newPriceTask;
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

        // Bước 2: giá bán mới (bảng giá đã kiểm duyệt trước, sau đó mới tới kết quả tìm Google).
        var newPrice = marketReferences.Count > 0
            ? PriceConditionFormula.Median(marketReferences.Select(x => x.PriceVndPerUnit).Where(x => x > 0).OrderBy(x => x).ToArray())
            : PriceConditionFormula.ConsensusNewPrice(searchedNewPrices.Items.Select(x => x.PriceVnd));
        if (newPrice is not > 0)
            newPrice = null;
        var newPriceSampleCount = marketReferences.Count > 0 ? marketReferences.Count : searchedNewPrices.Items.Count;

        // Bước 1: giá máy cũ quy về tình trạng tốt.
        var usingEquivalentEvidence = !hasExactUsedEvidence;
        var normalizedUsedPrices = usingEquivalentEvidence
            ? NormalizeSamples(equivalentTrades, equivalentListings, relatedExternalItems)
            : NormalizeSamples(trades, listings, exactExternalItems);
        var usedPrices = PriceConditionFormula.FilterUsedPrices(normalizedUsedPrices, newPrice);
        var usedSampleCount = usedPrices.Length;
        decimal? usedGoodPrice = usedSampleCount > 0 ? PriceConditionFormula.Median(usedPrices) : null;

        var response = CreateBaseResponse(
            trades.Count,
            listings.Count,
            exactExternalItems.Count,
            equivalentTrades.Count,
            equivalentListings.Count,
            relatedExternalItems.Count,
            newPriceSampleCount,
            unknownTradeSamples,
            external.FromCache,
            remaining,
            quota.ResetsAt,
            exactExternalItems.Concat(relatedExternalItems).ToArray(),
            marketReferences,
            searchedNewPrices.Items);

        // Bước 3: giá gốc cho máy tình trạng tốt.
        var annualRate = PriceConditionFormula.AnnualDepreciationRate(product.ProductTypeName);
        var usageYears = product.UsageDuration;
        var remainingRatio = PriceConditionFormula.RemainingValueRatio(annualRate, usageYears ?? 1);
        decimal? depreciatedNewPrice = newPrice * remainingRatio;

        string method;
        decimal basePrice;
        var adjustments = new List<AiPriceAdjustment>();
        if (usedGoodPrice is not null &&
            (usedSampleCount >= ReliableUsedSampleCount || depreciatedNewPrice is null))
        {
            method = "USED_MARKET";
            basePrice = usedGoodPrice.Value;
        }
        else if (usedGoodPrice is not null && depreciatedNewPrice is not null)
        {
            method = "BLENDED";
            basePrice = (usedGoodPrice.Value + depreciatedNewPrice.Value) / 2;
        }
        else if (newPrice is not null)
        {
            method = "NEW_PRICE_DEPRECIATION";
            basePrice = newPrice.Value;
            adjustments.Add(new AiPriceAdjustment(
                usageYears is null
                    ? $"Khấu hao tạm tính 1 năm (chưa nhập thời gian sử dụng) × {FormatPercent(annualRate)}/năm"
                    : $"Khấu hao {usageYears} năm × {FormatPercent(annualRate)}/năm",
                ToPercentChange(remainingRatio),
                null));
        }
        else
        {
            response.Status = "NO_RELIABLE_DATA";
            response.ReasonCodes = ["NO_RELIABLE_EVIDENCE"];
            response.Explanation =
                "Chưa tìm được giá đồ cũ cùng loại, cùng hãng hoặc giá bán mới đủ tin cậy để tính giá tham khảo.";
            return response;
        }

        if (!reserved)
        {
            var reservedRemaining = await quota.ReserveAsync(userId, dailyLimit, cancellationToken);
            if (reservedRemaining is null)
                return CreateLimitResponse(quota, dailyLimit);
            response.RemainingToday = reservedRemaining.Value;
        }

        // Bước 4: trừ theo tình trạng người bán chọn, tối đa 85% giá bán mới.
        var functionalityFactor = PriceConditionFormula.FunctionalityFactor(product.FunctionalityStatus);
        var damageFactor = PriceConditionFormula.DamageFactor(product.DamageLevel);
        var conditionFactor = PriceConditionFormula.ConditionFactor(product.FunctionalityStatus, product.DamageLevel);
        var functionalityLabel = PriceConditionFormula.FunctionalityLabel(product.FunctionalityStatus);
        var damageLabel = PriceConditionFormula.DamageLabel(product.DamageLevel);
        if (conditionFactor > functionalityFactor * damageFactor)
        {
            adjustments.Add(new AiPriceAdjustment(
                $"{functionalityLabel}, {damageLabel} (tính theo giá thanh lý)",
                ToPercentChange(conditionFactor),
                null));
        }
        else
        {
            adjustments.Add(new AiPriceAdjustment(functionalityLabel, ToPercentChange(functionalityFactor), null));
            adjustments.Add(new AiPriceAdjustment(damageLabel, ToPercentChange(damageFactor), null));
        }

        var workingPrice = method == "NEW_PRICE_DEPRECIATION"
            ? basePrice * remainingRatio * conditionFactor
            : basePrice * conditionFactor;
        if (newPrice is not null)
        {
            var cap = newPrice.Value * PriceConditionFormula.MaxUsedToNewPriceRatio;
            if (workingPrice > cap)
            {
                adjustments.Add(new AiPriceAdjustment(
                    $"Giới hạn tối đa {FormatPercent(PriceConditionFormula.MaxUsedToNewPriceRatio)} giá bán mới",
                    null,
                    RoundNearest(cap - workingPrice)));
                workingPrice = cap;
            }
        }

        var suggestedPrice = Math.Max(10_000m, RoundNearest(workingPrice));
        response.Status = "SUGGESTED";
        response.SuggestedPrice = suggestedPrice;
        response.MinPrice = Math.Max(10_000m, RoundDown(suggestedPrice * (1m - PriceConditionFormula.SuggestedRangeRatio)));
        response.MaxPrice = RoundUp(suggestedPrice * (1m + PriceConditionFormula.SuggestedRangeRatio));
        response.Confidence = GetConfidence(method, usingEquivalentEvidence, trades.Count, usedSampleCount);
        response.ReasonCodes = BuildReasonCodes(
            method,
            usingEquivalentEvidence,
            trades.Count + equivalentTrades.Count,
            listings.Count + equivalentListings.Count,
            exactExternalItems.Count + relatedExternalItems.Count,
            newPrice is not null,
            usedSampleCount,
            unknownTradeSamples);
        response.Explanation = BuildExplanation(method, usingEquivalentEvidence, usedSampleCount, usageYears);
        response.Breakdown = new AiPriceBreakdown
        {
            Method = method,
            BaseLabel = method switch
            {
                "BLENDED" => "Giá tham chiếu tình trạng tốt (máy cũ và giá mới đã khấu hao)",
                "NEW_PRICE_DEPRECIATION" => "Giá bán mới",
                _ => usingEquivalentEvidence
                    ? "Giá máy cũ model tương đương, tình trạng tốt"
                    : "Giá máy cũ cùng model, tình trạng tốt"
            },
            BasePrice = RoundNearest(basePrice),
            Adjustments = adjustments,
            FinalPrice = suggestedPrice,
            NewPriceReference = newPrice is null ? null : RoundNearest(newPrice.Value)
        };
        return response;
    }

    private static IEnumerable<decimal> NormalizeSamples(
        IEnumerable<CompletedPriceEvidence> trades,
        IEnumerable<ListingPriceEvidence> listings,
        IEnumerable<ExternalUsedPriceEvidence> externalItems)
    {
        // Tin trên mạng không có tình trạng chuẩn hóa nên giữ nguyên giá.
        return trades
            .Select(x => PriceConditionFormula.NormalizeToGoodCondition(x.Price, x.FunctionalityStatus, x.DamageLevel))
            .Concat(listings.Select(x =>
                PriceConditionFormula.NormalizeToGoodCondition(x.Price, x.FunctionalityStatus, x.DamageLevel)))
            .Concat(externalItems.Select(x => (decimal?)x.PriceVnd))
            .Where(x => x is > 0)
            .Select(x => x!.Value);
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
        IReadOnlyList<MarketPriceEvidence> marketSources,
        IReadOnlyList<NewPriceEvidence> searchedNewPriceSources)
    {
        var newPriceSources = marketSources.Count > 0
            ? marketSources.Take(3).Select(x => new AiPriceSource(
                "NEW_MARKET_REFERENCE", x.SourceName, x.SourceUrl, AsUtcOffset(x.ObservedAt)))
            : searchedNewPriceSources.Take(3).Select(x => new AiPriceSource(
                "NEW_MARKET_REFERENCE", x.SourceName, x.SourceUrl, x.RetrievedAt));

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
                .Concat(newPriceSources)
                .ToList()
        };
    }

    private static AiPriceSuggestionResponse CreateLimitResponse(IPriceSuggestionQuota quota, int dailyLimit) => new()
    {
        Status = "DAILY_LIMIT_REACHED",
        Explanation = $"Bạn đã dùng hết {dailyLimit} lượt gợi ý giá hôm nay.",
        RemainingToday = 0,
        ResetsAt = quota.ResetsAt
    };

    private static string GetConfidence(
        string method,
        bool usingEquivalentEvidence,
        int completedTrades,
        int usedSampleCount)
    {
        if (method != "USED_MARKET" || usingEquivalentEvidence)
            return "LOW";
        if (completedTrades >= ReliableUsedSampleCount)
            return "HIGH";
        return completedTrades >= 1 || usedSampleCount >= ReliableUsedSampleCount ? "MEDIUM" : "LOW";
    }

    private static List<string> BuildReasonCodes(
        string method,
        bool usingEquivalentEvidence,
        int tradeSamples,
        int listingSamples,
        int externalSamples,
        bool hasNewPrice,
        int usedSampleCount,
        int unknownAttributeSamples)
    {
        var codes = new List<string>();
        if (method != "NEW_PRICE_DEPRECIATION")
        {
            if (tradeSamples > 0)
                codes.Add("COMPLETED_TRADE_REFERENCE");
            if (listingSamples > 0 && !usingEquivalentEvidence)
                codes.Add("INTERNAL_LISTING_REFERENCE");
            if (externalSamples > 0)
                codes.Add("EXTERNAL_USED_LISTING_REFERENCE");
            if (usingEquivalentEvidence)
                codes.Add("EQUIVALENT_MODEL_REFERENCE");
        }
        if (hasNewPrice)
            codes.Add("NEW_MARKET_PRICE_REFERENCE");
        if (usedSampleCount < ReliableUsedSampleCount)
            codes.Add("LIMITED_EVIDENCE");
        if (unknownAttributeSamples > 0 && method != "NEW_PRICE_DEPRECIATION")
            codes.Add("ATTRIBUTE_MATCH_UNKNOWN");
        return codes;
    }

    private static string BuildExplanation(
        string method,
        bool usingEquivalentEvidence,
        int usedSampleCount,
        int? usageYears)
    {
        var usedSource = usingEquivalentEvidence ? "model tương đương" : "cùng model";
        return method switch
        {
            "USED_MARKET" =>
                $"Tính từ {usedSampleCount} mẫu giá đồ cũ {usedSource}, quy về máy tình trạng tốt rồi trừ theo tình trạng bạn chọn.",
            "BLENDED" =>
                $"Chỉ có {usedSampleCount} mẫu giá đồ cũ {usedSource} nên kết hợp với giá bán mới đã khấu hao, rồi trừ theo tình trạng bạn chọn.",
            _ => usageYears is null
                ? "Chưa có giá đồ cũ nên tính từ giá bán mới, tạm khấu hao 1 năm vì chưa nhập thời gian sử dụng, rồi trừ theo tình trạng bạn chọn."
                : $"Chưa có giá đồ cũ nên tính từ giá bán mới, khấu hao theo {usageYears} năm sử dụng rồi trừ theo tình trạng bạn chọn."
        };
    }

    // Hệ số 0,87 → −13 (%).
    private static decimal ToPercentChange(decimal factor) => Math.Round((factor - 1m) * 100m, 1);

    private static string FormatPercent(decimal ratio) =>
        $"{Math.Round(ratio * 100m, 1).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')}%";

    private static decimal RoundNearest(decimal value) =>
        Math.Round(value / 10_000m, 0, MidpointRounding.AwayFromZero) * 10_000m;

    private static decimal RoundDown(decimal value) => Math.Floor(value / 10_000m) * 10_000m;
    private static decimal RoundUp(decimal value) => Math.Ceiling(value / 10_000m) * 10_000m;

    private static DateTimeOffset AsUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record EvaluatedTrade(CompletedPriceEvidence Evidence, AttributeMatchResult Match);
}
