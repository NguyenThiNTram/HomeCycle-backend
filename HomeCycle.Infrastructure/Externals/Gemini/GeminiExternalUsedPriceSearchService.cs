using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.GenAI.Types;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Matching;
using HomeCycle.Application.Pricing.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.Externals.Gemini;

public sealed class GeminiExternalUsedPriceSearchService(
    GeminiRequestService gemini,
    IOptions<GeminiOptions> options,
    IMemoryCache cache,
    TimeProvider clock,
    ILogger<GeminiExternalUsedPriceSearchService> logger) : IExternalUsedPriceSearchService
{
    public async Task<ExternalUsedPriceSearchResult> SearchAsync(
        DynamicProductContext product,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.ExternalUsedPriceSearchEnabled || !settings.SearchGroundingEnabled)
            return ExternalUsedPriceSearchResult.Empty;

        var cacheKey = CreateCacheKey(product);
        if (cache.TryGetValue(cacheKey, out ExternalUsedPriceSearchResult? cached) && cached is not null)
            return cached with { FromCache = true };

        var stage = "grounding";
        try
        {
            var groundingResponses = new List<GenerateContentResponse>();
            var responseTexts = new List<string>();
            var prompts = ExternalUsedPriceSearchPrompt.BuildGroundingPrompts(product);

            for (var index = 0; index < prompts.Count; index++)
            {
                var attempt = await TrySearchAsync(
                    prompts[index],
                    index + 1,
                    settings,
                    cancellationToken);
                if (attempt is null)
                    continue;

                groundingResponses.Add(attempt.Response);
                if (!string.IsNullOrWhiteSpace(attempt.ResponseText))
                    responseTexts.Add($"SEARCH_RESULT_{index + 1}:\n{attempt.ResponseText}");
            }

            var maxResults = Math.Clamp(settings.ExternalUsedPriceSearchMaxResults, 1, 5);
            var groundedSources = BuildGroundedSources(groundingResponses, maxResults);
            var responseText = string.Join("\n\n", responseTexts);

            logger.LogInformation(
                "Gemini external grounding diagnostics: searchCallCount={SearchCallCount}, successfulSearchCallCount={SuccessfulSearchCallCount}, groundedSourceCount={GroundedSourceCount}, responseLength={ResponseLength}",
                prompts.Count,
                groundingResponses.Count,
                groundedSources.Count,
                responseText.Length);

            if (groundedSources.Count == 0 || string.IsNullOrWhiteSpace(responseText))
            {
                var emptyResult = new ExternalUsedPriceSearchResult(
                    [], groundedSources.Count > 0, false);
                CacheResult(cacheKey, emptyResult, settings, hasAcceptedEvidence: false);
                return emptyResult;
            }

            stage = "extraction";
            var sourceWhitelist = groundedSources
                .Select(x => new GroundedSourcePromptReference(x.SourceId, x.Title))
                .ToArray();

            using var extractionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            extractionTimeout.CancelAfter(TimeSpan.FromSeconds(
                Math.Max(1, settings.ExternalUsedPriceExtractionTimeoutSeconds)));

            var extractionResponse = await gemini.GenerateContentAsync(
                settings.MarketSearchModel,
                ExternalUsedPriceSearchPrompt.BuildExtractionPrompt(
                    product,
                    responseText,
                    sourceWhitelist),
                new GenerateContentConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = CreateResponseSchema(maxResults, groundedSources),
                    Temperature = 0.1,
                    MaxOutputTokens = Math.Clamp(
                        settings.ExternalUsedPriceExtractionMaxOutputTokens, 100, 600)
                },
                extractionTimeout.Token);

            logger.LogInformation(
                "Gemini external extraction usage: prompt={PromptTokens}, output={OutputTokens}, total={TotalTokens}",
                extractionResponse.UsageMetadata?.PromptTokenCount,
                extractionResponse.UsageMetadata?.CandidatesTokenCount,
                extractionResponse.UsageMetadata?.TotalTokenCount);

            var sourceMap = groundedSources.ToDictionary(
                x => x.SourceId,
                StringComparer.OrdinalIgnoreCase);
            var items = ParseItems(
                extractionResponse.Text,
                sourceMap,
                product.Model,
                maxResults,
                clock.GetUtcNow(),
                out var generatedItemCount,
                allowRelatedByProductContext: true);

            logger.LogInformation(
                "Gemini external extraction diagnostics: step2GeneratedItemCount={GeneratedItemCount}, step2AcceptedItemCount={AcceptedItemCount}, exactOrVariantCount={ExactOrVariantCount}, relatedCount={RelatedCount}",
                generatedItemCount,
                items.Count,
                items.Count(x => x.ModelMatchLevel is ExternalModelMatchLevel.Exact or ExternalModelMatchLevel.Variant),
                items.Count(x => x.ModelMatchLevel == ExternalModelMatchLevel.Related));

            var result = new ExternalUsedPriceSearchResult(items, true, false);
            CacheResult(cacheKey, result, settings, hasAcceptedEvidence: items.Count > 0);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini external used-price {Stage} timed out", stage);
            return ExternalUsedPriceSearchResult.Empty;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini external used-price {Stage} returned invalid JSON", stage);
            return ExternalUsedPriceSearchResult.Empty;
        }
        catch (Google.GenAI.ClientError exception)
        {
            logger.LogWarning(exception, "Gemini external used-price {Stage} failed", stage);
            return ExternalUsedPriceSearchResult.Empty;
        }
    }

    private async Task<SearchAttemptResult?> TrySearchAsync(
        string prompt,
        int attemptNumber,
        GeminiOptions settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(
                Math.Max(1, settings.ExternalUsedPriceSearchTimeoutSeconds)));

            var response = await gemini.GenerateContentAsync(
                settings.MarketSearchModel,
                prompt,
                new GenerateContentConfig
                {
                    Tools = [new Tool { GoogleSearch = new GoogleSearch() }],
                    Temperature = 0.1,
                    MaxOutputTokens = Math.Clamp(
                        settings.ExternalUsedPriceSearchMaxOutputTokens, 100, 600)
                },
                timeout.Token);

            logger.LogInformation(
                "Gemini external grounding call {AttemptNumber} usage: prompt={PromptTokens}, output={OutputTokens}, tool={ToolTokens}, total={TotalTokens}",
                attemptNumber,
                response.UsageMetadata?.PromptTokenCount,
                response.UsageMetadata?.CandidatesTokenCount,
                response.UsageMetadata?.ToolUsePromptTokenCount,
                response.UsageMetadata?.TotalTokenCount);

            return new SearchAttemptResult(response, response.Text?.Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Gemini external grounding call {AttemptNumber} timed out",
                attemptNumber);
            return null;
        }
        catch (Google.GenAI.ClientError exception)
        {
            logger.LogWarning(
                exception,
                "Gemini external grounding call {AttemptNumber} failed",
                attemptNumber);
            return null;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Gemini external grounding call {AttemptNumber} failed unexpectedly",
                attemptNumber);
            return null;
        }
    }

    private static IReadOnlyList<GroundedSource> BuildGroundedSources(
        IEnumerable<GenerateContentResponse> responses,
        int maxResults)
    {
        var sourcesBySearch = responses
            .Select(response => response.Candidates ?? [])
            .Select(candidates => candidates
                .SelectMany(candidate => candidate.GroundingMetadata?.GroundingChunks ?? [])
                .Where(chunk => chunk.Web is not null)
                .Select(chunk => chunk.Web!)
                .Where(web => TryNormalizeHttpUrl(web.Uri, out _))
                .GroupBy(web => NormalizeUrl(web.Uri!), StringComparer.OrdinalIgnoreCase)
                .Select(group => new SourceCandidate(
                    NormalizeSourceTitle(group.First().Title),
                    group.Key))
                .ToArray())
            .ToArray();

        var selected = new List<SourceCandidate>(maxResults);
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var largestSearch = sourcesBySearch.Length == 0 ? 0 : sourcesBySearch.Max(x => x.Length);

        for (var position = 0; position < largestSearch && selected.Count < maxResults; position++)
        {
            foreach (var sources in sourcesBySearch)
            {
                if (position >= sources.Length || !seenUrls.Add(sources[position].Url))
                    continue;

                selected.Add(sources[position]);
                if (selected.Count == maxResults)
                    break;
            }
        }

        return selected
            .Select((source, index) => new GroundedSource(
                $"S{index + 1}", source.Title, source.Url))
            .ToArray();
    }

    private static object CreateResponseSchema(
        int configuredMaxResults,
        IReadOnlyList<GroundedSource> groundedSources) => new
    {
        type = "object",
        properties = new
        {
            items = new
            {
                type = "array",
                maxItems = Math.Min(Math.Clamp(configuredMaxResults, 1, 5), groundedSources.Count),
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        priceVnd = new { type = "integer", minimum = 1 },
                        condition = new
                        {
                            anyOf = new object[] { new { type = "string" }, new { type = "null" } }
                        },
                        sourceId = new
                        {
                            type = "string",
                            @enum = groundedSources.Select(x => x.SourceId).ToArray()
                        },
                        observedModel = new { type = "string" },
                        brandMatched = new { type = "boolean" },
                        productTypeMatched = new { type = "boolean" },
                        attributesCompatible = new { type = "boolean" },
                        isUsed = new { type = "boolean" },
                        isWholeProduct = new { type = "boolean" }
                    },
                    required = new[]
                    {
                        "priceVnd", "condition", "sourceId", "observedModel", "brandMatched",
                        "productTypeMatched", "attributesCompatible", "isUsed", "isWholeProduct"
                    },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "items" },
        additionalProperties = false
    };

    private static IReadOnlyList<ExternalUsedPriceEvidence> ParseItems(
        string? responseText,
        IReadOnlyDictionary<string, GroundedSource> groundedSources,
        string requestedModel,
        int maxResults,
        DateTimeOffset retrievedAt,
        out int generatedItemCount,
        bool allowRelatedByProductContext = false)
    {
        generatedItemCount = 0;

        if (string.IsNullOrWhiteSpace(responseText) || groundedSources.Count == 0)
            return [];

        using var document = JsonDocument.Parse(responseText);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return [];

        generatedItemCount = items.GetArrayLength();

        var accepted = new List<ExternalUsedPriceEvidence>();
        var seenSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("priceVnd", out var priceElement) ||
                !priceElement.TryGetDecimal(out var price) ||
                price <= 0 ||
                !ReadRequiredTrue(item, "brandMatched") ||
                !ReadRequiredTrue(item, "productTypeMatched") ||
                !ReadRequiredTrue(item, "isUsed") ||
                !ReadRequiredTrue(item, "isWholeProduct") ||
                !item.TryGetProperty("observedModel", out var modelElement) ||
                modelElement.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("sourceId", out var sourceIdElement) ||
                sourceIdElement.ValueKind != JsonValueKind.String)
                continue;

            var observedModel = modelElement.GetString()?.Trim();
            observedModel ??= string.Empty;
            if (observedModel.Length > 100)
                observedModel = observedModel[..100];

            var attributesCompatible = ReadRequiredTrue(item, "attributesCompatible");
            var (matchLevel, similarity) = ClassifyModel(requestedModel, observedModel);
            if (matchLevel == ExternalModelMatchLevel.Unrelated &&
                allowRelatedByProductContext && attributesCompatible)
                matchLevel = ExternalModelMatchLevel.Related;
            if (matchLevel == ExternalModelMatchLevel.Unrelated)
                continue;

            var sourceId = sourceIdElement.GetString();
            if (string.IsNullOrWhiteSpace(sourceId) ||
                !groundedSources.TryGetValue(sourceId, out var source) ||
                !seenSourceIds.Add(sourceId))
                continue;

            string? condition = null;
            if (item.TryGetProperty("condition", out var conditionElement) &&
                conditionElement.ValueKind == JsonValueKind.String)
            {
                condition = conditionElement.GetString()?.Trim();
                if (condition?.Length > 200)
                    condition = condition[..200];
            }

            accepted.Add(new ExternalUsedPriceEvidence(
                Math.Round(price, 0),
                observedModel,
                matchLevel,
                similarity,
                string.IsNullOrWhiteSpace(condition) ? null : condition,
                source.Title,
                source.Url,
                retrievedAt));
        }

        var exactOrVariant = accepted
            .Where(x => x.ModelMatchLevel is ExternalModelMatchLevel.Exact or ExternalModelMatchLevel.Variant)
            .OrderByDescending(x => x.ModelMatchLevel)
            .ThenByDescending(x => x.ModelSimilarity)
            .Take(maxResults)
            .ToArray();

        return exactOrVariant.Length > 0
            ? exactOrVariant
            : accepted
                .Where(x => x.ModelMatchLevel == ExternalModelMatchLevel.Related)
                .OrderByDescending(x => x.ModelSimilarity)
                .Take(maxResults)
                .ToArray();
    }

    private static (ExternalModelMatchLevel Level, decimal Similarity) ClassifyModel(
        string requestedModel,
        string observedModel)
    {
        var requested = NormalizeModel(requestedModel);
        var observed = NormalizeModel(observedModel);
        if (requested.Length == 0 || observed.Length == 0)
            return (ExternalModelMatchLevel.Unrelated, 0m);

        if (StringComparer.Ordinal.Equals(requested, observed))
            return (ExternalModelMatchLevel.Exact, 1m);

        var similarity = CalculateModelSimilarity(requested, observed);
        if (IsRegionalVariant(requested, observed))
            return (ExternalModelMatchLevel.Variant, Math.Max(0.95m, similarity));

        return (similarity >= 0.25m)
            ? (ExternalModelMatchLevel.Related, similarity)
            : (ExternalModelMatchLevel.Unrelated, similarity);
    }

    private static bool IsRegionalVariant(string requested, string observed)
    {
        var shorter = requested.Length <= observed.Length ? requested : observed;
        var longer = requested.Length > observed.Length ? requested : observed;
        if (!longer.StartsWith(shorter, StringComparison.Ordinal))
            return false;

        var suffix = longer[shorter.Length..];
        return suffix.Length is >= 1 and <= 3 && suffix.All(char.IsAsciiLetter);
    }

    private static decimal CalculateModelSimilarity(string left, string right)
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
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        var similarity = 1m - (decimal)previous[right.Length] / maxLength;
        return Math.Round(Math.Max(0m, similarity), 3);
    }

    private static string NormalizeModel(string value) =>
        new(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsAsciiLetterOrDigit)
            .ToArray());

    private void CacheResult(
        string cacheKey,
        ExternalUsedPriceSearchResult result,
        GeminiOptions settings,
        bool hasAcceptedEvidence)
    {
        var duration = hasAcceptedEvidence
            ? TimeSpan.FromHours(Math.Clamp(settings.ExternalUsedPriceSearchCacheHours, 1, 24))
            : TimeSpan.FromMinutes(Math.Clamp(settings.ExternalUsedPriceNegativeCacheMinutes, 1, 1_440));
        cache.Set(cacheKey, result, duration);
    }

    private static bool ReadRequiredTrue(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.True;

    private static string CreateCacheKey(DynamicProductContext product)
    {
        var attributes = product.Attributes
            .OrderBy(x => x.AttributeId)
            .Select(x => $"{x.AttributeId:D}:{AttributeValueNormalizer.Normalize(x)}");
        var rawKey = string.Join("|",
            product.ProductTypeId.ToString("D"),
            product.BrandId.ToString("D"),
            product.Model,
            string.Join(';', attributes));
        return "gemini:external-used-price:v4:" +
               Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
    }

    private static bool TryNormalizeHttpUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
            return false;

        normalized = NormalizeUrl(uri.AbsoluteUri);
        return true;
    }

    private static string NormalizeSourceTitle(string? value)
    {
        var title = string.IsNullOrWhiteSpace(value) ? "Nguồn tham khảo" : value.Trim();
        return title.Length <= 200 ? title : title[..200];
    }

    private static string NormalizeUrl(string value) => value.Trim().TrimEnd('/');

    private sealed record SearchAttemptResult(GenerateContentResponse Response, string? ResponseText);
    private sealed record SourceCandidate(string Title, string Url);
    private sealed record GroundedSource(string SourceId, string Title, string Url);
}

