using System.Text.Json;
using Google.GenAI.Types;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.Externals.Gemini;

public sealed class GeminiSupplierMatchReranker(
    GeminiRequestService gemini,
    IOptions<GeminiOptions> options,
    ILogger<GeminiSupplierMatchReranker> logger) : ISupplierMatchAiReranker
{
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.Ordinal)
    {
        "STRONG_OVERALL_MATCH", "MODEL_COMPATIBLE", "ATTRIBUTE_COMPATIBLE",
        "BUDGET_COMPATIBLE", "QUANTITY_COMPATIBLE", "CONDITION_COMPATIBLE",
        "LOCATION_COMPATIBLE", "SELLER_REPUTATION", "LIMITED_INFORMATION"
    };

    public async Task<SupplierMatchAiResult?> RerankAsync(
        SupplierDemandContext demand,
        IReadOnlyList<SupplierMatchAiCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.SupplierMatchRerankingEnabled || candidates.Count == 0)
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.SupplierMatchRerankingTimeoutSeconds)));
        try
        {
            var aliases = candidates.Select(candidate => candidate.Alias).Distinct(StringComparer.Ordinal).ToArray();
            var configuredMaximum = Math.Clamp(
                settings.SupplierMatchRerankingMaxOutputTokens, 500, 2000);
            var maxOutputTokens = Math.Clamp(
                300 + candidates.Count * 80, 500, configuredMaximum);
            var response = await gemini.GenerateContentAsync(
                settings.BuyMatchingModel,
                SupplierMatchRerankPrompt.Build(demand, candidates),
                new GenerateContentConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = CreateSchema(aliases),
                    Temperature = 0.1,
                    MaxOutputTokens = maxOutputTokens
                },
                timeout.Token);
            var responseText = response.Text?.Trim();
            if (!LooksLikeCompleteJsonObject(responseText))
            {
                logger.LogWarning(
                    "Gemini supplier reranking returned incomplete JSON: candidateCount={CandidateCount}, responseCharacters={ResponseCharacters}, promptTokens={PromptTokens}, outputTokens={OutputTokens}",
                    candidates.Count, responseText?.Length ?? 0,
                    response.UsageMetadata?.PromptTokenCount,
                    response.UsageMetadata?.CandidatesTokenCount);
                return null;
            }

            var parsed = Parse(responseText);
            logger.LogInformation(
                "Gemini supplier reranking completed: candidateCount={CandidateCount}, generatedRankingCount={RankingCount}, promptTokens={PromptTokens}, outputTokens={OutputTokens}",
                candidates.Count, parsed?.Rankings.Count ?? 0,
                response.UsageMetadata?.PromptTokenCount, response.UsageMetadata?.CandidatesTokenCount);
            return parsed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini supplier reranking timed out");
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini supplier reranking returned invalid JSON");
            return null;
        }
        catch (Google.GenAI.ClientError exception)
        {
            logger.LogWarning(exception, "Gemini supplier reranking request failed");
            return null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Gemini supplier reranking failed unexpectedly");
            return null;
        }
    }

    private static object CreateSchema(IReadOnlyCollection<string> aliases) => new
    {
        type = "object",
        properties = new
        {
            rankings = new
            {
                type = "array",
                minItems = 1,
                maxItems = aliases.Count,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        alias = new { type = "string", @enum = aliases.ToArray() },
                        aiScore = new { type = "number", minimum = 0, maximum = 10 },
                        reasonCodes = new
                        {
                            type = "array", minItems = 1, maxItems = 3,
                            items = new { type = "string", @enum = AllowedReasons.ToArray() }
                        },
                        shortExplanation = new
                        {
                            type = "string",
                            maxLength = 160,
                            description = "Một câu tiếng Việt ngắn giải thích lý do xếp hạng."
                        }
                    },
                    required = new[] { "alias", "aiScore", "reasonCodes", "shortExplanation" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "rankings" },
        additionalProperties = false
    };

    private static SupplierMatchAiResult? Parse(string? text)
    {
        if (!LooksLikeCompleteJsonObject(text)) return null;
        using var document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("rankings", out var rankings) ||
            rankings.ValueKind != JsonValueKind.Array)
            return null;

        var items = new List<SupplierMatchAiDecision>();
        foreach (var item in rankings.EnumerateArray())
        {
            if (!item.TryGetProperty("alias", out var aliasElement) ||
                !item.TryGetProperty("aiScore", out var scoreElement) ||
                !item.TryGetProperty("reasonCodes", out var reasonsElement) ||
                !item.TryGetProperty("shortExplanation", out var explanationElement) ||
                aliasElement.ValueKind != JsonValueKind.String ||
                !scoreElement.TryGetDecimal(out var score) ||
                reasonsElement.ValueKind != JsonValueKind.Array ||
                explanationElement.ValueKind != JsonValueKind.String)
                continue;

            var alias = aliasElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(alias) || score is < 0m or > 10m) continue;
            var reasons = reasonsElement.EnumerateArray()
                .Where(reason => reason.ValueKind == JsonValueKind.String)
                .Select(reason => reason.GetString())
                .Where(reason => reason is not null && AllowedReasons.Contains(reason))
                .Select(reason => reason!)
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (reasons.Length == 0) continue;
            var explanation = explanationElement.GetString()?.Trim() ?? string.Empty;
            if (explanation.Length > 160) explanation = explanation[..160];
            items.Add(new SupplierMatchAiDecision(alias, score, reasons, explanation));
        }
        return items.Count == 0 ? null : new SupplierMatchAiResult(items);
    }

    private static bool LooksLikeCompleteJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var value = text.AsSpan().Trim();
        return value.Length >= 2 && value[0] == '{' && value[^1] == '}';
    }
}
