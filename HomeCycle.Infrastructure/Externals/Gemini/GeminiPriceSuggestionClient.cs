using System.Text.Json;
using Google.GenAI.Types;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.Externals.Gemini;

public sealed class GeminiPriceSuggestionClient(
    GeminiRequestService gemini,
    IOptions<GeminiOptions> options,
    ILogger<GeminiPriceSuggestionClient> logger) : IPriceSuggestionAiClient
{
    public async Task<AiPriceDecision?> SuggestAsync(
        PriceSuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));

        try
        {
            var response = await gemini.GenerateContentAsync(
                settings.PriceSuggestionModel,
                PriceSuggestionPrompt.Build(context),
                new GenerateContentConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = CreateResponseSchema(),
                    Temperature = 0.1,
                    MaxOutputTokens = 400
                },
                timeout.Token);

            logger.LogInformation(
                "Gemini price suggestion usage for {Model}: prompt={PromptTokens}, output={OutputTokens}, total={TotalTokens}",
                settings.PriceSuggestionModel,
                response.UsageMetadata?.PromptTokenCount,
                response.UsageMetadata?.CandidatesTokenCount,
                response.UsageMetadata?.TotalTokenCount);

            return Parse(response.Text);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini price suggestion timed out");
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini price suggestion returned invalid JSON");
            return null;
        }
        catch (Google.GenAI.ClientError exception)
        {
            logger.LogWarning(exception, "Gemini price suggestion request failed");
            return null;
        }
    }

    private static object CreateResponseSchema() => new
    {
        type = "object",
        properties = new
        {
            suggestedPrice = NullableInteger(),
            minPrice = NullableInteger(),
            maxPrice = NullableInteger(),
            reasonCodes = new
            {
                type = "array",
                minItems = 1,
                maxItems = 4,
                items = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "COMPLETED_TRADE_REFERENCE",
                        "INTERNAL_LISTING_REFERENCE",
                        "EXTERNAL_USED_LISTING_REFERENCE",
                        "EQUIVALENT_MODEL_REFERENCE",
                        "NEW_MARKET_PRICE_REFERENCE",
                        "LIMITED_EVIDENCE",
                        "ATTRIBUTE_MATCH_UNKNOWN",
                        "NO_RELIABLE_EVIDENCE"
                    }
                }
            },
            shortExplanation = new { type = "string" }
        },
        required = new[]
        {
            "suggestedPrice", "minPrice", "maxPrice", "reasonCodes", "shortExplanation"
        },
        additionalProperties = false
    };

    private static object NullableInteger() => new
    {
        anyOf = new object[]
        {
            new { type = "integer", minimum = 1 },
            new { type = "null" }
        }
    };

    private static AiPriceDecision? Parse(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        if (!root.TryGetProperty("reasonCodes", out var reasonsElement) ||
            reasonsElement.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("shortExplanation", out var explanationElement) ||
            explanationElement.ValueKind != JsonValueKind.String)
            return null;

        var reasons = reasonsElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AiPriceDecision(
            ReadNullablePrice(root, "suggestedPrice"),
            ReadNullablePrice(root, "minPrice"),
            ReadNullablePrice(root, "maxPrice"),
            reasons,
            explanationElement.GetString()?.Trim() ?? string.Empty);
    }

    private static decimal? ReadNullablePrice(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null)
            return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var price)
            ? price
            : null;
    }
}
