using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.GenAI.Types;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Matching;
using HomeCycle.Application.Pricing.Models;
using HomeCycle.Application.Pricing.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.Externals.Gemini;

// Tìm giá bán mới của đúng model ở tối đa 3 nhà bán lẻ qua Google Search.
// Cache theo model + thuộc tính (không theo tình trạng) nên đổi tình trạng không phải tìm lại.
public sealed class GeminiNewPriceSearchService(
    GeminiRequestService gemini,
    IOptions<GeminiOptions> options,
    IMemoryCache cache,
    TimeProvider clock,
    ILogger<GeminiNewPriceSearchService> logger) : INewPriceSearchService
{
    public async Task<NewPriceSearchResult> SearchAsync(
        DynamicProductContext product,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.NewPriceSearchEnabled || !settings.SearchGroundingEnabled)
            return NewPriceSearchResult.Empty;

        var cacheKey = CreateCacheKey(product);
        if (cache.TryGetValue(cacheKey, out NewPriceSearchResult? cached) && cached is not null)
            return cached with { FromCache = true };

        var maxSources = Math.Clamp(settings.NewPriceSearchMaxSources, 2, 5);
        var stage = "grounding";
        try
        {
            using var searchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            searchTimeout.CancelAfter(TimeSpan.FromSeconds(
                Math.Max(1, settings.ExternalUsedPriceSearchTimeoutSeconds)));

            var groundingResponse = await PriceSearchGeminiCall.GenerateGroundedAsync(
                gemini,
                settings,
                NewPriceSearchPrompt.BuildGroundingPrompt(product, maxSources),
                new GenerateContentConfig
                {
                    Tools = [new Tool { GoogleSearch = new GoogleSearch() }],
                    Temperature = 0.1,
                    MaxOutputTokens = Math.Clamp(settings.ExternalUsedPriceSearchMaxOutputTokens, 100, 600)
                },
                logger,
                "new-price grounding",
                searchTimeout.Token);

            var groundedSources = GeminiExternalUsedPriceSearchService.BuildGroundedSources(
                [groundingResponse], maxSources, PriceSourceCatalog.AcceptsNewPrice);
            var responseText = groundingResponse.Text?.Trim() ?? string.Empty;

            logger.LogInformation(
                "Gemini new-price grounding diagnostics: groundedSourceCount={GroundedSourceCount}, responseLength={ResponseLength}, returnedDomains=[{ReturnedDomains}]",
                groundedSources.Count,
                responseText.Length,
                GeminiExternalUsedPriceSearchService.DescribeGroundingDomains([groundingResponse]));

            if (groundedSources.Count == 0 || responseText.Length == 0)
            {
                CacheResult(cacheKey, NewPriceSearchResult.Empty, settings, hasItems: false);
                return NewPriceSearchResult.Empty;
            }

            stage = "extraction";
            using var extractionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            extractionTimeout.CancelAfter(TimeSpan.FromSeconds(
                Math.Max(1, settings.ExternalUsedPriceExtractionTimeoutSeconds)));

            var extractionResponse = await PriceSearchGeminiCall.GenerateAsync(
                gemini,
                settings,
                NewPriceSearchPrompt.BuildExtractionPrompt(
                    product,
                    responseText,
                    groundedSources.Select(x => new GroundedSourcePromptReference(x.SourceId, x.Title)).ToArray()),
                new GenerateContentConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = CreateResponseSchema(groundedSources),
                    Temperature = 0.1,
                    MaxOutputTokens = Math.Clamp(settings.ExternalUsedPriceExtractionMaxOutputTokens, 100, 600)
                },
                logger,
                "new-price extraction",
                extractionTimeout.Token);

            var items = ParseItems(
                extractionResponse.Text,
                groundedSources.ToDictionary(x => x.SourceId, StringComparer.OrdinalIgnoreCase),
                product.Model,
                clock.GetUtcNow(),
                out var generatedItemCount,
                out var rejections);

            logger.LogInformation(
                "Gemini new-price extraction diagnostics: generatedItemCount={GeneratedItemCount}, acceptedItemCount={AcceptedItemCount}, rejected=[{Rejections}]",
                generatedItemCount,
                items.Count,
                string.Join("; ", rejections));

            var result = new NewPriceSearchResult(items, false);
            CacheResult(cacheKey, result, settings, hasItems: items.Count > 0);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini new-price {Stage} timed out", stage);
            return NewPriceSearchResult.Empty;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini new-price {Stage} returned invalid JSON", stage);
            return NewPriceSearchResult.Empty;
        }
        catch (Google.GenAI.ClientError exception)
        {
            logger.LogWarning(exception, "Gemini new-price {Stage} failed", stage);
            return NewPriceSearchResult.Empty;
        }
    }

    private static object CreateResponseSchema(
        IReadOnlyList<GeminiExternalUsedPriceSearchService.GroundedSource> groundedSources) => new
    {
        type = "object",
        properties = new
        {
            items = new
            {
                type = "array",
                maxItems = groundedSources.Count,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        priceVnd = new { type = "integer", minimum = 1 },
                        sourceId = new
                        {
                            type = "string",
                            @enum = groundedSources.Select(x => x.SourceId).ToArray()
                        },
                        observedModel = new { type = "string" },
                        brandMatched = new { type = "boolean" },
                        isNew = new { type = "boolean" },
                        isWholeProduct = new { type = "boolean" }
                    },
                    required = new[]
                    {
                        "priceVnd", "sourceId", "observedModel", "brandMatched", "isNew", "isWholeProduct"
                    },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "items" },
        additionalProperties = false
    };

    private static IReadOnlyList<NewPriceEvidence> ParseItems(
        string? responseText,
        IReadOnlyDictionary<string, GeminiExternalUsedPriceSearchService.GroundedSource> groundedSources,
        string requestedModel,
        DateTimeOffset retrievedAt,
        out int generatedItemCount,
        out List<string> rejections)
    {
        generatedItemCount = 0;
        rejections = [];
        if (string.IsNullOrWhiteSpace(responseText) || groundedSources.Count == 0)
            return [];

        using var document = JsonDocument.Parse(responseText);
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return [];

        generatedItemCount = items.GetArrayLength();
        var accepted = new List<NewPriceEvidence>();
        var seenSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("priceVnd", out var priceElement) ||
                !priceElement.TryGetDecimal(out var price) || price <= 0)
            {
                rejections.Add("no-price");
                continue;
            }

            var flags = new[] { "brandMatched", "isNew", "isWholeProduct" }
                .Where(flag => !ReadRequiredTrue(item, flag))
                .ToArray();
            if (flags.Length > 0)
            {
                rejections.Add($"flags-false:{string.Join(",", flags)}");
                continue;
            }

            if (!item.TryGetProperty("observedModel", out var modelElement) ||
                modelElement.ValueKind != JsonValueKind.String ||
                !item.TryGetProperty("sourceId", out var sourceIdElement) ||
                sourceIdElement.ValueKind != JsonValueKind.String)
            {
                rejections.Add("missing-model-or-source");
                continue;
            }

            // Giá mới chỉ nhận đúng model hoặc biến thể hậu tố; model gần giống dễ lệch giá.
            var observedModel = modelElement.GetString()?.Trim() ?? string.Empty;
            var (matchLevel, _) = GeminiExternalUsedPriceSearchService.ClassifyModel(requestedModel, observedModel);
            if (matchLevel is not (ExternalModelMatchLevel.Exact or ExternalModelMatchLevel.Variant))
            {
                rejections.Add($"model-mismatch:'{observedModel}'");
                continue;
            }

            var sourceId = sourceIdElement.GetString();
            if (string.IsNullOrWhiteSpace(sourceId) ||
                !groundedSources.TryGetValue(sourceId, out var source) ||
                !seenSourceIds.Add(sourceId))
            {
                rejections.Add($"source:'{sourceId}'");
                continue;
            }

            accepted.Add(new NewPriceEvidence(
                Math.Round(price, 0),
                observedModel.Length > 100 ? observedModel[..100] : observedModel,
                source.Title,
                source.Url,
                retrievedAt,
                source.Tier,
                source.Domain));
        }

        return accepted;
    }

    private void CacheResult(
        string cacheKey,
        NewPriceSearchResult result,
        GeminiOptions settings,
        bool hasItems)
    {
        var duration = hasItems
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
        return "gemini:new-price:v4:" +
               Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
    }
}
