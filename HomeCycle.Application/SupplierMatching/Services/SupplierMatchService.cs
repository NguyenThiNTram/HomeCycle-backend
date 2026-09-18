using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.DTOs.Responses.SupplierMatching;
using HomeCycle.Application.Interfaces.Repositories.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Application.SupplierMatching.Scoring;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.SupplierMatching.Services;

public sealed class SupplierMatchService(
    ISupplierMatchCandidateRepository candidates,
    ISupplierMatchEntitlementService entitlements,
    ISupplierMatchAiReranker aiReranker,
    ISupplierMatchQuota quota,
    ISupplierMatchCache cache,
    SupplierMatchScorer scorer,
    IMediaService mediaService,
    TimeProvider clock) : ISupplierMatchService
{
    private const int CandidateLimit = 150;
    private const string PostMediaTargetType = "Post";

    public Task<SupplierMatchResponse> MatchDraftAsync(
        Guid requesterId,
        SupplierMatchDraftRequest request,
        CancellationToken cancellationToken = default) =>
        MatchAsync(SupplierDemandContextBuilder.FromDraft(requesterId, request), 0, int.MaxValue, cancellationToken);

    public async Task<SupplierMatchResponse> MatchAsync(
        SupplierDemandContext demand,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        var entitlement = demand.RequesterId.HasValue
            ? await entitlements.GetAsync(demand.RequesterId.Value, cancellationToken)
            : SupplierMatchEntitlement.Free();
        var effectiveDemand = entitlement.AdvancedFiltersEnabled
            ? demand
            : demand with { AdvancedFilters = SupplierMatchAdvancedFilters.None };
        var fingerprint = SupplierMatchFingerprint.Create(effectiveDemand, entitlement.Tier);

        var cached = await TryRestoreCacheAsync(fingerprint, effectiveDemand, cancellationToken);
        if (cached is not null)
        {
            var status = await GetQuotaStatusAsync(demand.RequesterId, entitlement, cancellationToken);
            return await BuildResponseAsync(cached.Ranked, entitlement, status, skip, take,
                cached.RankingSource, cached.AiStatus, true, effectiveDemand, cancellationToken);
        }

        await using var fingerprintLock = await cache.AcquireAsync(fingerprint, cancellationToken);
        cached = await TryRestoreCacheAsync(fingerprint, effectiveDemand, cancellationToken);
        if (cached is not null)
        {
            var status = await GetQuotaStatusAsync(demand.RequesterId, entitlement, cancellationToken);
            return await BuildResponseAsync(cached.Ranked, entitlement, status, skip, take,
                cached.RankingSource, cached.AiStatus, true, effectiveDemand, cancellationToken);
        }

        var backendRanked = await GetBackendRankedAsync(effectiveDemand, entitlement.ResultLimit, cancellationToken);
        if (backendRanked.Length <= 1 || !demand.RequesterId.HasValue || !entitlement.AiRerankingEnabled)
        {
            var status = await GetQuotaStatusAsync(demand.RequesterId, entitlement, cancellationToken);
            return await BuildResponseAsync(backendRanked, entitlement, status, skip, take,
                "BACKEND_ONLY", backendRanked.Length <= 1 ? "NOT_REQUIRED" : "NOT_ELIGIBLE",
                false, effectiveDemand, cancellationToken);
        }

        var reservation = await quota.TryReserveAsync(
            demand.RequesterId.Value, entitlement.DailyAiRefreshLimit, cancellationToken);
        if (!reservation.Reserved)
            return await BuildResponseAsync(backendRanked, entitlement, ToStatus(reservation), skip, take,
                "BACKEND_ONLY", "DAILY_LIMIT_REACHED", false, effectiveDemand, cancellationToken);

        var aiResult = await aiReranker.RerankAsync(
            effectiveDemand,
            backendRanked.Select((item, index) => ToAiCandidate(effectiveDemand, item, index)).ToArray(),
            cancellationToken);
        var application = ApplyAiRankings(backendRanked, aiResult);
        if (application is null)
        {
            cache.SetFallback(fingerprint, CreateBackendCacheEntry(backendRanked, "FALLBACK"));
            return await BuildResponseAsync(backendRanked, entitlement, ToStatus(reservation), skip, take,
                "BACKEND_ONLY", "FALLBACK", false, effectiveDemand, cancellationToken);
        }

        cache.Set(fingerprint, new SupplierMatchCacheEntry(
            application.CacheDecisions, clock.GetUtcNow(), "AI_RERANKED", "AVAILABLE"));
        return await BuildResponseAsync(application.Ranked, entitlement, ToStatus(reservation), skip, take,
            "AI_RERANKED", "AVAILABLE", false, effectiveDemand, cancellationToken);
    }

    public async Task<SupplierMatchResponse> MatchBackendOnlyAsync(
        SupplierDemandContext demand,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        var entitlement = demand.RequesterId.HasValue
            ? await entitlements.GetAsync(demand.RequesterId.Value, cancellationToken)
            : SupplierMatchEntitlement.Free();
        var effectiveDemand = entitlement.AdvancedFiltersEnabled
            ? demand
            : demand with { AdvancedFilters = SupplierMatchAdvancedFilters.None };
        var ranked = await GetBackendRankedAsync(
            effectiveDemand, entitlement.ResultLimit, cancellationToken);
        var status = await GetQuotaStatusAsync(demand.RequesterId, entitlement, cancellationToken);
        return await BuildResponseAsync(ranked, entitlement, status, skip, take,
            "BACKEND_ONLY", "NOT_REQUIRED", false, effectiveDemand, cancellationToken);
    }

    public async Task<SupplierMatchResponse> MatchInitialBackendAsync(
        SupplierDemandContext demand,
        int take,
        CancellationToken cancellationToken = default)
    {
        var entitlement = demand.RequesterId.HasValue
            ? await entitlements.GetAsync(demand.RequesterId.Value, cancellationToken)
            : SupplierMatchEntitlement.Free();
        var basicDemand = demand with
        {
            ModelNumber = null,
            NormalizedModelNumber = null,
            FunctionalityStatus = null,
            DamageLevel = null,
            UsageDuration = null,
            City = null,
            Attributes = [],
            AdvancedFilters = SupplierMatchAdvancedFilters.None
        };
        var ranked = await GetBackendRankedAsync(basicDemand, Math.Min(3, take), cancellationToken);
        var status = await GetQuotaStatusAsync(demand.RequesterId, entitlement, cancellationToken);
        return await BuildResponseAsync(ranked, entitlement, status, 0, Math.Min(3, take),
            "BACKEND_ONLY", "NOT_REQUIRED", false, basicDemand, cancellationToken);
    }

    private async Task<RestoredCache?> TryRestoreCacheAsync(
        string fingerprint,
        SupplierDemandContext demand,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGet(fingerprint, out var entry) || entry?.Decisions.Count is not > 0)
            return null;

        var ids = entry.Decisions.Select(decision => decision.SellPostId).Distinct().ToArray();
        var liveStates = await candidates.GetLiveStatesAsync(ids, cancellationToken);
        var validIds = liveStates
            .Where(state => state.IsActive && state.AvailableQuantity > 0 && state.Price is > 0)
            .Select(state => state.SellPostId)
            .ToHashSet();
        if (validIds.Count == 0)
        {
            cache.Remove(fingerprint);
            return null;
        }

        var currentCandidates = await candidates.GetCandidatesByIdsAsync(demand, validIds, cancellationToken);
        var currentById = currentCandidates.ToDictionary(candidate => candidate.SellPostId);
        var restored = new List<RankedCandidate>(entry.Decisions.Count);
        var retainedDecisions = new List<SupplierMatchCachedDecision>(entry.Decisions.Count);
        foreach (var decision in entry.Decisions.OrderBy(item => item.Rank))
        {
            if (!currentById.TryGetValue(decision.SellPostId, out var candidate)) continue;
            var evaluation = scorer.Evaluate(demand, candidate);
            if (evaluation.IsDisqualified || evaluation.BaseScore < 4m) continue;
            restored.Add(new RankedCandidate(
                candidate,
                evaluation,
                CalculateFinalScore(evaluation.BaseScore, decision.AiScore),
                decision.AiScore,
                decision.ReasonCodes,
                decision.ShortExplanation,
                decision.Rank));
            retainedDecisions.Add(decision);
        }

        if (restored.Count == 0)
        {
            cache.Remove(fingerprint);
            return null;
        }

        if (retainedDecisions.Count != entry.Decisions.Count)
        {
            var retainedEntry = entry with { Decisions = retainedDecisions };
            if (entry.AiStatus == "FALLBACK")
                cache.SetFallback(fingerprint, retainedEntry);
            else
                cache.Set(fingerprint, retainedEntry);
        }

        var ranked = restored
            .OrderByDescending(item => item.FinalScore ?? item.Evaluation.BaseScore)
            .ThenBy(item => item.CachedRank)
            .ThenByDescending(item => item.Candidate.CreatedAt)
            .ThenBy(item => item.Candidate.SellPostId)
            .ToArray();
        return new RestoredCache(ranked, entry.RankingSource, entry.AiStatus);
    }

    private async Task<RankedCandidate[]> GetBackendRankedAsync(
        SupplierDemandContext demand,
        int resultLimit,
        CancellationToken cancellationToken)
    {
        var source = await candidates.GetCandidatesAsync(demand, CandidateLimit, cancellationToken);
        return source
            .Select(candidate => new RankedCandidate(candidate, scorer.Evaluate(demand, candidate)))
            .Where(item => !item.Evaluation.IsDisqualified && item.Evaluation.BaseScore >= 4m)
            .OrderByDescending(item => item.Evaluation.BaseScore)
            .ThenByDescending(item => item.Candidate.CreatedAt)
            .ThenBy(item => item.Candidate.SellPostId)
            .Take(Math.Clamp(resultLimit, 1, 100))
            .ToArray();
    }

    private async Task<SupplierMatchQuotaStatus> GetQuotaStatusAsync(
        Guid? userId,
        SupplierMatchEntitlement entitlement,
        CancellationToken cancellationToken) =>
        userId.HasValue
            ? await quota.GetRemainingAsync(userId.Value, entitlement.DailyAiRefreshLimit, cancellationToken)
            : new SupplierMatchQuotaStatus(
                entitlement.DailyAiRefreshLimit, 0, NextVietnamMidnight(clock.GetUtcNow()));

    private async Task<SupplierMatchResponse> BuildResponseAsync(
        IReadOnlyList<RankedCandidate> ranked,
        SupplierMatchEntitlement entitlement,
        SupplierMatchQuotaStatus quotaStatus,
        int skip,
        int take,
        string rankingSource,
        string aiStatus,
        bool fromCache,
        SupplierDemandContext demand,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, entitlement.ResultLimit);
        var page = ranked.Skip(skip).Take(take).ToArray();
        var mediaResult = await mediaService.GetByTargetsAsync(
            page.Select(item => item.Candidate.SellPostId).ToArray(), PostMediaTargetType, cancellationToken);
        var media = mediaResult.IsSuccess && mediaResult.Data is not null
            ? mediaResult.Data
            : new Dictionary<Guid, IReadOnlyList<MediaResponse>>();
        return new SupplierMatchResponse
        {
            Tier = entitlement.Tier.ToString().ToUpperInvariant(),
            RankingSource = rankingSource,
            AiStatus = aiStatus,
            AdvancedFiltersApplied = entitlement.AdvancedFiltersEnabled &&
                                     demand.AdvancedFilters != SupplierMatchAdvancedFilters.None,
            ResultLimit = entitlement.ResultLimit,
            RemainingAiRefreshes = quotaStatus.Remaining,
            ResetsAt = quotaStatus.ResetsAt,
            FromCache = fromCache,
            CandidateCount = ranked.Count,
            GeneratedAt = clock.GetUtcNow(),
            Matches = page.Select(item => ToResponse(item, media, rankingSource)).ToArray()
        };
    }

    private static SupplierMatchAiCandidate ToAiCandidate(
        SupplierDemandContext demand,
        RankedCandidate item,
        int index)
    {
        var evaluation = item.Evaluation;
        var conditionWeight =
            (demand.FunctionalityStatus.HasValue ? 0.4m : 0m) +
            (demand.DamageLevel.HasValue ? 0.4m : 0m) +
            (demand.UsageDuration.HasValue ? 0.2m : 0m);
        bool? cityMatched = string.IsNullOrWhiteSpace(demand.City)
            ? null
            : evaluation.MatchedCriteria.Contains("CITY", StringComparer.Ordinal)
                ? true
                : evaluation.UnmatchedCriteria.Contains("CITY", StringComparer.Ordinal)
                    ? false
                    : null;
        return new SupplierMatchAiCandidate(
            $"C{index + 1}",
            evaluation.BaseScore,
            evaluation.ModelMatchLevel.ToString().ToUpperInvariant(),
            demand.PriceFrom.HasValue || demand.PriceTo.HasValue
                ? Math.Clamp(evaluation.Breakdown.Budget / 1.5m, 0m, 1m)
                : null,
            Math.Clamp(evaluation.Breakdown.Quantity, 0m, 1m),
            evaluation.AttributeStates.Count(pair => pair.Value == SupplierCriterionState.Matched),
            evaluation.AttributeStates.Count(pair => pair.Value == SupplierCriterionState.Conflicted),
            evaluation.AttributeStates.Count(pair => pair.Value == SupplierCriterionState.Unknown),
            conditionWeight > 0
                ? Math.Clamp(evaluation.Breakdown.Condition / conditionWeight, 0m, 1m)
                : null,
            cityMatched,
            item.Candidate.AverageRating,
            evaluation.ReasonCodes);
    }

    private SupplierMatchCacheEntry CreateBackendCacheEntry(
        IReadOnlyList<RankedCandidate> ranked,
        string aiStatus) => new(
        ranked.Select((item, index) => new SupplierMatchCachedDecision(
            item.Candidate.SellPostId, index, null, [], null)).ToArray(),
        clock.GetUtcNow(),
        "BACKEND_ONLY",
        aiStatus);

    private static AiApplication? ApplyAiRankings(
        IReadOnlyList<RankedCandidate> backendRanked,
        SupplierMatchAiResult? result)
    {
        if (result?.Rankings is not { Count: > 0 }) return null;
        var aliases = backendRanked
            .Select((candidate, index) => new { Alias = $"C{index + 1}", Candidate = candidate })
            .ToDictionary(item => item.Alias, item => item.Candidate, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var accepted = new List<RankedCandidate>(backendRanked.Count);
        foreach (var decision in result.Rankings)
        {
            if (!aliases.TryGetValue(decision.Alias, out var original) ||
                !seen.Add(decision.Alias) || decision.AiScore is < 0m or > 10m)
                continue;
            accepted.Add(original with
            {
                FinalScore = CalculateFinalScore(original.Evaluation.BaseScore, decision.AiScore),
                AiScore = decision.AiScore,
                AiReasonCodes = decision.ReasonCodes,
                AiExplanation = decision.ShortExplanation
            });
        }
        if (accepted.Count == 0) return null;
        accepted.AddRange(aliases.Where(pair => !seen.Contains(pair.Key)).Select(pair => pair.Value));
        var ranked = accepted
            .OrderByDescending(item => item.FinalScore ?? item.Evaluation.BaseScore)
            .ThenByDescending(item => item.Candidate.CreatedAt)
            .ThenBy(item => item.Candidate.SellPostId)
            .ToArray();
        var cacheDecisions = ranked.Select((item, index) => new SupplierMatchCachedDecision(
            item.Candidate.SellPostId, index, item.AiScore,
            item.AiReasonCodes ?? [], item.AiExplanation)).ToArray();
        return new AiApplication(ranked, cacheDecisions);
    }

    private static decimal? CalculateFinalScore(decimal backendScore, decimal? aiScore)
    {
        if (!aiScore.HasValue) return null;
        var blended = Math.Round(backendScore * 0.7m + aiScore.Value * 0.3m, 2);
        return Math.Clamp(blended, Math.Max(0m, backendScore - 1.5m), Math.Min(10m, backendScore + 1.5m));
    }

    private static BuyPostMatchResponse ToResponse(
        RankedCandidate item,
        IReadOnlyDictionary<Guid, IReadOnlyList<MediaResponse>> media,
        string rankingSource)
    {
        var candidate = item.Candidate;
        var evaluation = item.Evaluation;
        media.TryGetValue(candidate.SellPostId, out var files);
        return new BuyPostMatchResponse
        {
            SellPost = new PostResponse
            {
                PostId = candidate.SellPostId, OwnerId = candidate.SupplierId,
                ProductId = candidate.ProductId, ProductName = candidate.ProductName,
                ProductTypeName = candidate.ProductTypeName, CategoryName = candidate.CategoryName,
                BrandName = candidate.BrandName, OwnerName = candidate.SupplierName,
                AvatarUrl = candidate.SupplierAvatarUrl, AverageRating = candidate.AverageRating,
                TotalReviews = candidate.TotalReviews, Quantity = candidate.RemainingQuantity,
                RemainingQuantity = candidate.AvailableQuantity, PostType = PostType.Sell,
                BasePrice = candidate.Price, Status = PostStatus.Active, City = candidate.City,
                CreatedAt = candidate.CreatedAt, ExpiryDate = candidate.ExpiryDate, Medias = files ?? []
            },
            MatchSummary = BuyPostMatching.FromEvaluation(evaluation),
            MatchingScore = item.FinalScore ?? evaluation.BaseScore,
            MatchingLevel = ToLevel(item.FinalScore ?? evaluation.BaseScore),
            RankingSource = item.AiExplanation is null ? "BACKEND_ONLY" : rankingSource,
            ReasonCodes = evaluation.ReasonCodes.Concat(item.AiReasonCodes ?? [])
                .Distinct(StringComparer.Ordinal).ToList(),
            ShortExplanation = item.AiExplanation ?? BuildExplanation(evaluation),
            CanFulfillQuantity = evaluation.CanFulfillQuantity
        };
    }

    private static string ToLevel(decimal score) => score >= 8m ? "HIGH" : score >= 6m ? "MEDIUM" : "LOW";

    private static string BuildExplanation(SupplierMatchEvaluation evaluation)
    {
        var highlights = new List<string>();
        if (evaluation.ReasonCodes.Contains("EXACT_MODEL", StringComparer.Ordinal)) highlights.Add("đúng model");
        else if (evaluation.ReasonCodes.Contains("MODEL_VARIANT", StringComparer.Ordinal)) highlights.Add("model cùng biến thể");
        if (evaluation.ReasonCodes.Contains("WITHIN_BUDGET", StringComparer.Ordinal)) highlights.Add("nằm trong ngân sách");
        if (evaluation.ReasonCodes.Contains("FULL_QUANTITY_AVAILABLE", StringComparer.Ordinal)) highlights.Add("đủ số lượng");
        if (highlights.Count == 0) highlights.Add("các tiêu chí sản phẩm đã cung cấp");
        return $"Phù hợp {evaluation.BaseScore:0.##}/10 dựa trên {string.Join(", ", highlights.Take(3))}.";
    }

    private static DateTimeOffset NextVietnamMidnight(DateTimeOffset now)
    {
        var vietnamNow = now.ToOffset(TimeSpan.FromHours(7));
        return new DateTimeOffset(vietnamNow.Date.AddDays(1), TimeSpan.FromHours(7));
    }

    private static SupplierMatchQuotaStatus ToStatus(SupplierMatchQuotaReservation reservation) =>
        new(reservation.Limit, reservation.Remaining, reservation.ResetsAt);

    private sealed record RankedCandidate(
        SupplierCandidate Candidate,
        SupplierMatchEvaluation Evaluation,
        decimal? FinalScore = null,
        decimal? AiScore = null,
        IReadOnlyList<string>? AiReasonCodes = null,
        string? AiExplanation = null,
        int CachedRank = int.MaxValue);

    private sealed record AiApplication(
        RankedCandidate[] Ranked,
        IReadOnlyList<SupplierMatchCachedDecision> CacheDecisions);

    private sealed record RestoredCache(
        RankedCandidate[] Ranked,
        string RankingSource,
        string AiStatus);
}
