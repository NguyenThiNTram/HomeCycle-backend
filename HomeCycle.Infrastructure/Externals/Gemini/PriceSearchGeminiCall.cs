using System.Diagnostics;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Infrastructure.Externals.Gemini;

// Lệnh Gemini cho phần tìm giá, có ghi log thời gian.
// - Lệnh trích giá (chỉ đọc chữ có sẵn): đặt mức suy nghĩ thấp để trả lời nhanh; model không nhận
//   mức này thì gọi lại một lần không kèm thiết lập.
// - Lệnh tìm Google: giữ mức suy nghĩ mặc định, vì ở mức thấp model hay trả lời luôn mà không tìm.
//   Nếu vẫn không có nguồn nào thì thử lại một lần với yêu cầu bắt buộc dùng Google Search.
internal static class PriceSearchGeminiCall
{
    private const string ForceSearchPrefix =
        "Bắt buộc dùng công cụ Google Search để tìm thông tin mới nhất trước khi trả lời; " +
        "không trả lời từ kiến thức có sẵn. ";

    public static async Task<GenerateContentResponse> GenerateGroundedAsync(
        GeminiRequestService gemini,
        GeminiOptions settings,
        string prompt,
        GenerateContentConfig config,
        ILogger logger,
        string stage,
        CancellationToken cancellationToken)
    {
        var response = await GenerateAsync(
            gemini, settings, prompt, config, logger, stage, cancellationToken, applyThinkingLevel: false);
        if (CountGroundingChunks(response) > 0)
            return response;

        logger.LogInformation(
            "Gemini {Stage} returned no search sources; retrying with forced search. Answer: {Answer}",
            stage,
            Snippet(response.Text));
        var retry = await GenerateAsync(
            gemini, settings, ForceSearchPrefix + prompt, config, logger, stage + " retry", cancellationToken,
            applyThinkingLevel: false);
        if (CountGroundingChunks(retry) == 0)
            logger.LogInformation("Gemini {Stage} retry still returned no search sources. Answer: {Answer}",
                stage,
                Snippet(retry.Text));
        return retry;
    }

    private static string Snippet(string? text)
    {
        var value = (text ?? string.Empty).Replace('\n', ' ').Trim();
        return value.Length <= 300 ? value : value[..300] + "…";
    }

    public static int CountGroundingChunks(GenerateContentResponse response) =>
        (response.Candidates ?? [])
            .Sum(candidate => candidate.GroundingMetadata?.GroundingChunks?.Count(chunk => chunk.Web is not null) ?? 0);

    public static async Task<GenerateContentResponse> GenerateAsync(
        GeminiRequestService gemini,
        GeminiOptions settings,
        string prompt,
        GenerateContentConfig config,
        ILogger logger,
        string stage,
        CancellationToken cancellationToken,
        bool applyThinkingLevel = true)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var thinkingLevel = applyThinkingLevel ? ParseThinkingLevel(settings.PriceSearchThinkingLevel) : null;
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
