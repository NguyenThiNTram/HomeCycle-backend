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
    PriceSuggestionOptions options,
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
        // Giá giao dịch/tin đăng HomeCycle chỉ được dùng khi bật cấu hình (xem PriceSuggestionOptions).
        var useHomeCycleEvidence = options.UseHomeCyclePriceEvidence;
        var completedCandidates = useHomeCycleEvidence
            ? await evidenceRepository.GetCompletedAsync(
                product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken)
            : Array.Empty<CompletedPriceEvidence>();

        var evaluatedTrades = completedCandidates
            .Select(item => new EvaluatedTrade(item, attributeMatcher.Match(product.Attributes, item.Attributes)))
            .Where(x => x.Match.Level != AttributeMatchLevel.Conflicted)
            .Take(120)
            .ToList();
        var unknownTradeSamples = evaluatedTrades.Count(x => x.Match.Level == AttributeMatchLevel.Unknown);
        var trades = evaluatedTrades.Select(x => x.Evidence).ToList();

        var listingCandidates = useHomeCycleEvidence
            ? await evidenceRepository.GetActiveAsync(
                product.ProductTypeId, product.BrandId, product.Model, now.UtcDateTime, cancellationToken)
            : Array.Empty<ListingPriceEvidence>();
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

        if (!hasExactUsedEvidence && useHomeCycleEvidence)
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

        // Bước 2: giá bán mới, lấy từ nguồn uy tín nhất có dữ liệu: bảng giá đã kiểm duyệt
        // → trang chính hãng → chuỗi bán lẻ lớn (ưu tiên ≥ 2 nguồn khớp nhau, cuối cùng mới tới 1 nguồn).
        var (newPrice, newPriceSourceLabel, newPriceOrigin, newPriceRetailers) =
            SelectNewPrice(marketReferences, searchedNewPrices.Items);
        if (newPrice is not > 0)
            newPrice = null;
        var newPriceSampleCount = marketReferences.Count > 0 ? marketReferences.Count : searchedNewPrices.Items.Count;

        // Bước 1: giá máy cũ quy về tình trạng tốt. Lấy lần lượt giao dịch HomeCycle → tin đăng
        // HomeCycle → chuỗi bán lẻ lớn → trang rao vặt, dừng khi đã đủ mẫu.
        var usingEquivalentEvidence = !hasExactUsedEvidence;
        var usedTiers = usingEquivalentEvidence
            // Mẫu web khác model chỉ hiển thị để tham khảo: hãng, loại hàng và độ tương thích của
            // chúng do Gemini tự đánh giá nên không đủ căn cứ để tính giá. Mẫu tương đương trên
            // HomeCycle được so thuộc tính bằng code nên vẫn dùng.
            ? BuildUsedTiers(equivalentTrades, equivalentListings, Array.Empty<ExternalUsedPriceEvidence>())
            : BuildUsedTiers(trades, listings, exactExternalItems);
        var (pooledUsedPrices, usedSourceTiers) = TakeMostTrusted(usedTiers);

        // Giá mới thấp hơn giá máy cũ tình trạng tốt là mâu thuẫn. Chỉ bỏ giá mới khi nó yếu (chỉ 1
        // nhà bán lẻ) và mâu thuẫn với dữ liệu HomeCycle. Các trường hợp khác giữ giá mới làm giới
        // hạn, để bộ lọc loại các mẫu máy cũ cao hơn giá mới và giá gợi ý không vượt giá mới.
        var homeCycleUsedPrices = usedTiers
            .Where(t => (t.Tier is PriceSourceTier.HomeCycleTrade or PriceSourceTier.HomeCycleListing) &&
                        usedSourceTiers.Contains(t.Tier))
            .SelectMany(t => t.Prices)
            .OrderBy(x => x)
            .ToArray();
        var newPriceConflict = false;
        if (newPrice is not null && newPriceOrigin == NewPriceOrigin.SingleRetailer &&
            homeCycleUsedPrices.Length > 0 &&
            newPrice.Value < PriceConditionFormula.Median(homeCycleUsedPrices))
        {
            newPrice = null;
            newPriceSourceLabel = null;
            newPriceOrigin = NewPriceOrigin.None;
            newPriceConflict = true;
        }

        var usedPrices = PriceConditionFormula.FilterUsedPrices(pooledUsedPrices, newPrice);
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
            quota.ResetsAt);
        response.Sources = BuildSources(
            exactExternalItems.Concat(relatedExternalItems).ToArray(),
            marketReferences,
            searchedNewPrices.Items,
            usedSourceTiers,
            usedPrices,
            newPrice is null ? NewPriceOrigin.None : newPriceOrigin,
            newPriceRetailers);

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
        response.Confidence = GetConfidence(
            method,
            usingEquivalentEvidence,
            usedSourceTiers.Contains(PriceSourceTier.HomeCycleTrade) ? trades.Count : 0,
            usedSampleCount,
            usedSourceTiers.Contains(PriceSourceTier.Classified) || newPriceConflict);
        response.ReasonCodes = BuildReasonCodes(
            method,
            usingEquivalentEvidence,
            usedSourceTiers.Contains(PriceSourceTier.HomeCycleTrade) ? 1 : 0,
            usedSourceTiers.Contains(PriceSourceTier.HomeCycleListing) ? 1 : 0,
            usedSourceTiers.Any(t => t is PriceSourceTier.MajorRetailer or PriceSourceTier.Classified) ? 1 : 0,
            newPrice is not null,
            usedSampleCount,
            unknownTradeSamples);
        response.Explanation = BuildExplanation(
            method, usingEquivalentEvidence, usedSampleCount, usageYears, usedSourceTiers, newPriceSourceLabel);
        if (newPriceConflict)
            response.Explanation +=
                " Giá bán mới tìm được (chỉ 1 nhà bán lẻ) thấp hơn giá máy cũ trên HomeCycle nên không được dùng; nên kiểm tra lại trước khi đăng.";
        response.Breakdown = new AiPriceBreakdown
        {
            Method = method,
            BaseLabel = method switch
            {
                "BLENDED" => "Giá tham chiếu tình trạng tốt (máy cũ và giá mới đã khấu hao)",
                "NEW_PRICE_DEPRECIATION" => $"Giá bán mới ({newPriceSourceLabel})",
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

    private static IReadOnlyList<(PriceSourceTier Tier, IReadOnlyList<decimal> Prices)> BuildUsedTiers(
        IEnumerable<CompletedPriceEvidence> trades,
        IEnumerable<ListingPriceEvidence> listings,
        IEnumerable<ExternalUsedPriceEvidence> externalItems)
    {
        var external = externalItems.ToList();
        return new (PriceSourceTier, IReadOnlyList<decimal>)[]
        {
            (PriceSourceTier.HomeCycleTrade,
                NormalizeToGood(trades.Select(x => (x.Price, x.FunctionalityStatus, x.DamageLevel)))),
            (PriceSourceTier.HomeCycleListing,
                NormalizeToGood(listings.Select(x => (x.Price, x.FunctionalityStatus, x.DamageLevel)))),
            // Tin trên mạng chưa có tình trạng chuẩn hóa nên giữ nguyên giá.
            // Mỗi nhà bán lẻ chỉ tính một mẫu; tin rao vặt là của từng người bán nên tính riêng từng tin.
            (PriceSourceTier.MajorRetailer, ExternalPricesPerDomain(external, PriceSourceTier.MajorRetailer)),
            (PriceSourceTier.Classified, ExternalPrices(external, PriceSourceTier.Classified))
        };
    }

    private static IReadOnlyList<decimal> NormalizeToGood(
        IEnumerable<(decimal Price, int? Functionality, int? Damage)> samples) =>
        samples
            .Select(x => PriceConditionFormula.NormalizeToGoodCondition(x.Price, x.Functionality, x.Damage))
            .Where(x => x is > 0)
            .Select(x => x!.Value)
            .ToArray();

    private static IReadOnlyList<decimal> ExternalPrices(
        IEnumerable<ExternalUsedPriceEvidence> items,
        PriceSourceTier tier) =>
        items
            .Where(x => x.SourceTier == tier && PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd))
            .Select(x => x.PriceVnd)
            .ToArray();

    private static IReadOnlyList<decimal> ExternalPricesPerDomain(
        IEnumerable<ExternalUsedPriceEvidence> items,
        PriceSourceTier tier) =>
        items
            .Where(x => x.SourceTier == tier && PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd))
            .GroupBy(x => x.SourceDomain ?? x.SourceName, StringComparer.OrdinalIgnoreCase)
            .Select(g => PriceConditionFormula.Median(g.Select(x => x.PriceVnd).OrderBy(x => x).ToArray()))
            .ToArray();

    // Gom mẫu từ nhóm uy tín nhất trở xuống, dừng khi đã đủ mẫu tin cậy.
    private static (List<decimal> Prices, List<PriceSourceTier> Tiers) TakeMostTrusted(
        IReadOnlyList<(PriceSourceTier Tier, IReadOnlyList<decimal> Prices)> tiers)
    {
        var prices = new List<decimal>();
        var usedTiers = new List<PriceSourceTier>();
        foreach (var (tier, values) in tiers)
        {
            if (values.Count == 0)
                continue;
            prices.AddRange(values);
            usedTiers.Add(tier);
            if (prices.Count >= ReliableUsedSampleCount)
                break;
        }

        return (prices, usedTiers);
    }

    private enum NewPriceOrigin { None, Verified, Official, RetailerConsensus, SingleRetailer }

    private static (decimal? Price, string? SourceLabel, NewPriceOrigin Origin, IReadOnlySet<string> Retailers)
        SelectNewPrice(IReadOnlyList<MarketPriceEvidence> verified, IReadOnlyList<NewPriceEvidence> searched)
    {
        var none = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var verifiedPrices = verified.Select(x => x.PriceVndPerUnit).Where(x => x > 0).OrderBy(x => x).ToArray();
        if (verifiedPrices.Length > 0)
            return (PriceConditionFormula.Median(verifiedPrices), "bảng giá đã kiểm duyệt", NewPriceOrigin.Verified, none);

        // Mỗi nhà bán lẻ chỉ tính là một nguồn, dù có nhiều trang sản phẩm.
        var official = PricePerDomain(searched, PriceSourceTier.OfficialBrand);
        var retail = PricePerDomain(searched, PriceSourceTier.MajorRetailer);
        var agreed = PriceConditionFormula.AgreeingNewPrices(retail.Select(x => x.Price));
        decimal? consensus = agreed.Length > 0 ? PriceConditionFormula.Median(agreed) : null;
        if (official.Length > 0)
        {
            var officialPrice = PriceConditionFormula.Median(official.Select(x => x.Price).ToArray());
            // Có giá đồng thuận của nhà bán lẻ thì giá chính hãng phải khớp (lệch ≤ 25%) mới được dùng.
            if (consensus is null ||
                Math.Abs(officialPrice - consensus.Value) <= consensus.Value * PriceConditionFormula.NewPriceOutlierBand)
                return (officialPrice, "trang chính hãng", NewPriceOrigin.Official, Keys(official));
        }

        if (consensus is not null)
        {
            var agreedRetailers = retail.Where(x => agreed.Contains(x.Price)).ToArray();
            return (consensus, $"{agreedRetailers.Length} nhà bán lẻ lớn", NewPriceOrigin.RetailerConsensus,
                Keys(agreedRetailers));
        }

        return retail.Length == 1
            ? (retail[0].Price, "1 nhà bán lẻ lớn", NewPriceOrigin.SingleRetailer, Keys(retail))
            : (null, null, NewPriceOrigin.None, none);

        static IReadOnlySet<string> Keys(IEnumerable<RetailerPrice> items) =>
            items.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record RetailerPrice(string Key, decimal Price);

    private static RetailerPrice[] PricePerDomain(IEnumerable<NewPriceEvidence> items, PriceSourceTier tier) =>
        items
            .Where(x => x.SourceTier == tier && PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd))
            .GroupBy(x => x.SourceDomain ?? x.SourceName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RetailerPrice(
                g.Key,
                PriceConditionFormula.Median(g.Select(x => x.PriceVnd).OrderBy(x => x).ToArray())))
            .OrderBy(x => x.Price)
            .ToArray();

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
        DateTimeOffset resetsAt) => new()
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
        }
    };

    // Danh sách nguồn trả cho app; UsedInCalculation cho biết nguồn có thực sự vào phép tính không.
    private static List<AiPriceSource> BuildSources(
        IReadOnlyList<ExternalUsedPriceEvidence> externalItems,
        IReadOnlyList<MarketPriceEvidence> marketSources,
        IReadOnlyList<NewPriceEvidence> searchedNewPriceSources,
        IReadOnlyList<PriceSourceTier> usedSourceTiers,
        IReadOnlyList<decimal> usedPrices,
        NewPriceOrigin newPriceOrigin,
        IReadOnlySet<string> newPriceRetailers)
    {
        // Giá máy cũ: nhà bán lẻ được tính bằng giá giữa của từng nhà bán lẻ, tin rao vặt tính từng tin.
        // Nguồn được coi là "dùng để tính" khi đúng giá đó còn nằm trong tập giá sau khi lọc.
        var keptPrices = usedPrices.ToHashSet();
        var retailerMedians = externalItems
            .Where(x => x.SourceTier == PriceSourceTier.MajorRetailer &&
                        x.ModelMatchLevel != ExternalModelMatchLevel.Related &&
                        PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd))
            .GroupBy(x => x.SourceDomain ?? x.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => PriceConditionFormula.Median(g.Select(x => x.PriceVnd).OrderBy(x => x).ToArray()),
                StringComparer.OrdinalIgnoreCase);

        var used = externalItems.OrderBy(x => x.SourceTier).Select(x =>
        {
            var referenceOnly = x.ModelMatchLevel == ExternalModelMatchLevel.Related;
            var contributedPrice = x.SourceTier == PriceSourceTier.MajorRetailer
                ? retailerMedians.GetValueOrDefault(x.SourceDomain ?? x.SourceName)
                : x.PriceVnd;
            var inCalculation = !referenceOnly &&
                                usedSourceTiers.Contains(x.SourceTier) &&
                                PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd) &&
                                keptPrices.Contains(contributedPrice);
            return new AiPriceSource(
                referenceOnly ? "EXTERNAL_EQUIVALENT_LISTING" : "EXTERNAL_USED_LISTING",
                x.SourceName,
                x.SourceUrl,
                x.RetrievedAt,
                referenceOnly
                    ? $"{PriceSourceCatalog.TrustLabel(x.SourceTier)} · chỉ tham khảo"
                    : PriceSourceCatalog.TrustLabel(x.SourceTier),
                inCalculation);
        });

        // Giá mới: chỉ những nhà bán lẻ thực sự tham gia tính giá mới được đánh dấu. Nguồn được
        // dùng luôn đứng trước và luôn được hiện đủ; còn chỗ (tối đa 3) mới thêm nguồn không dùng.
        IEnumerable<AiPriceSource> newPrices;
        if (marketSources.Count > 0)
        {
            // Mọi mục trong bảng giá đã kiểm duyệt (tối đa 10, giới hạn ở repository) đều vào phép tính.
            newPrices = marketSources.Select(x => new AiPriceSource(
                "NEW_MARKET_REFERENCE", x.SourceName, x.SourceUrl, AsUtcOffset(x.ObservedAt),
                "Bảng giá đã kiểm duyệt", newPriceOrigin == NewPriceOrigin.Verified));
        }
        else
        {
            var searched = searchedNewPriceSources
                .Select(x => (
                    Tier: x.SourceTier,
                    Source: new AiPriceSource(
                        "NEW_MARKET_REFERENCE", x.SourceName, x.SourceUrl, x.RetrievedAt,
                        PriceSourceCatalog.TrustLabel(x.SourceTier),
                        newPriceOrigin != NewPriceOrigin.None &&
                        PriceConditionFormula.IsWithinPostingLimits(x.PriceVnd) &&
                        newPriceRetailers.Contains(x.SourceDomain ?? x.SourceName))))
                .OrderByDescending(x => x.Source.UsedInCalculation)
                .ThenBy(x => x.Tier)
                .Select(x => x.Source)
                .ToList();
            var usedCount = searched.Count(x => x.UsedInCalculation);
            newPrices = searched.Take(Math.Max(3, usedCount));
        }

        return used.Concat(newPrices).ToList();
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
        int usedSampleCount,
        bool usesClassifiedListings)
    {
        // Dùng tới tin rao vặt thì độ tin cậy tối đa là thấp. Chỉ dựa trên giá rao (tin đăng,
        // nhà bán lẻ) mà chưa có giao dịch thật thì cũng chỉ ở mức thấp.
        if (method != "USED_MARKET" || usingEquivalentEvidence || usesClassifiedListings)
            return "LOW";
        if (completedTrades >= ReliableUsedSampleCount)
            return "HIGH";
        return completedTrades >= 1 && usedSampleCount >= ReliableUsedSampleCount ? "MEDIUM" : "LOW";
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
        int? usageYears,
        IReadOnlyList<PriceSourceTier> usedTiers,
        string? newPriceSource)
    {
        var usedSource = usingEquivalentEvidence ? "model tương đương" : "cùng model";
        var usedFrom = usedTiers.Count > 0
            ? $" ({string.Join(", ", usedTiers.Select(PriceSourceCatalog.TrustLabel))})"
            : string.Empty;
        var newFrom = string.IsNullOrWhiteSpace(newPriceSource) ? string.Empty : $" ({newPriceSource})";
        var askingOnly = method != "NEW_PRICE_DEPRECIATION" && !usedTiers.Contains(PriceSourceTier.HomeCycleTrade)
            ? " Chưa có giao dịch đã hoàn tất, các mẫu là giá đang rao nên chỉ để tham khảo."
            : string.Empty;
        return BuildExplanationCore() + askingOnly;

        string BuildExplanationCore() => method switch
        {
            "USED_MARKET" =>
                $"Tính từ {usedSampleCount} mẫu giá đồ cũ {usedSource}{usedFrom}, quy về máy tình trạng tốt rồi trừ theo tình trạng bạn chọn.",
            "BLENDED" =>
                $"Chỉ có {usedSampleCount} mẫu giá đồ cũ {usedSource}{usedFrom} nên kết hợp với giá bán mới{newFrom} đã khấu hao, rồi trừ theo tình trạng bạn chọn.",
            _ => usageYears is null
                ? $"Chưa có giá đồ cũ nên tính từ giá bán mới{newFrom}, tạm khấu hao 1 năm vì chưa nhập thời gian sử dụng, rồi trừ theo tình trạng bạn chọn."
                : $"Chưa có giá đồ cũ nên tính từ giá bán mới{newFrom}, khấu hao theo {usageYears} năm sử dụng rồi trừ theo tình trạng bạn chọn."
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
