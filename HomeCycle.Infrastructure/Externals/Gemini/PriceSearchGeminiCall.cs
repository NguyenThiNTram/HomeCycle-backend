using System.Diagnostics;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Infrastructure.Externals.Gemini;

// Lệnh Gemini cho phần tìm giá: đặt mức suy nghĩ thấp để trả lời nhanh và ghi log thời gian.
// Nếu model không nhận mức suy nghĩ đã cấu hình thì gọi lại một lần không kèm thiết lập này.
internal static class PriceSearchGeminiCall
{
    public static async Task<GenerateContentResponse> GenerateAsync(
        GeminiRequestService gemini,
        GeminiOptions settings,
        string prompt,
        GenerateContentConfig config,
        ILogger logger,
        string stage,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var thinkingLevel = ParseThinkingLevel(settings.PriceSearchThinkingLevel);
            if (thinkingLevel is not null)
            {
                config.ThinkingConfig = new ThinkingConfig { ThinkingLevel = thinkingLevel };
                try
                {
                    return await gemini.GenerateContentAsync(settings.MarketSearchModel, prompt, config, cancellationToken);
                }
                catch (Google.GenAI.ClientError exception)
                    when (exception.Message.Contains("thinking", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        exception,
                        "Gemini {Stage} rejected thinking level {ThinkingLevel}; retrying without it",
                        stage,
                        thinkingLevel.Value.Value);
                    config.ThinkingConfig = null;
                }
            }

            return await gemini.GenerateContentAsync(settings.MarketSearchModel, prompt, config, cancellationToken);
        }
        finally
        {
            logger.LogInformation(
                "Gemini {Stage} finished in {ElapsedMs} ms",
                stage,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static ThinkingLevel? ParseThinkingLevel(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "minimal" => ThinkingLevel.Minimal,
            "low" => ThinkingLevel.Low,
            _ => null
        };
}
