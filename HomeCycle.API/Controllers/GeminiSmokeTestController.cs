using Google.GenAI.Types;
using HomeCycle.Infrastructure.Externals.Gemini;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/test/gemini")]
//[Authorize(Roles = "Admin")]
public sealed class GeminiSmokeTestController(
    GeminiRequestService gemini,
    IOptions<GeminiOptions> options,
    IWebHostEnvironment environment,
    ILogger<GeminiSmokeTestController> logger) : ControllerBase
{
    [HttpGet("market-search")]
    public async Task<IActionResult> TestMarketSearch(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();

        var settings = options.Value;
        if (!settings.SearchGroundingEnabled)
            return BadRequest(new { success = false, message = "Search grounding đang bị tắt trong cấu hình." });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            var response = await gemini.GenerateContentAsync(
                model: settings.MarketSearchModel,
                contents: "Tìm trên web giá bán mới tại Việt Nam của máy giặt LG FV1410S4W. " +
                          "Chỉ trả giá của đúng model, tính bằng VND cho một máy. " +
                          "Nếu không có nguồn giá phù hợp, hãy nói rõ không tìm được.",
                config: new GenerateContentConfig
                {
                    Tools = [new Tool { GoogleSearch = new GoogleSearch() }],
                    Temperature = 0.1,
                    MaxOutputTokens = 500
                },
                cancellationToken: timeout.Token);

            var grounding = response.Candidates?.FirstOrDefault()?.GroundingMetadata;
            var sources = grounding?.GroundingChunks?
                .Where(chunk => chunk.Web != null)
                .Select(chunk => new { title = chunk.Web!.Title, url = chunk.Web.Uri })
                .ToArray() ?? [];

            logger.LogInformation(
                "Gemini market search smoke test completed for model {Model} with {SourceCount} sources",
                settings.MarketSearchModel,
                sources.Length);

            return Ok(new
            {
                success = !string.IsNullOrWhiteSpace(response.Text),
                model = settings.MarketSearchModel,
                grounded = sources.Length > 0,
                sourceCount = sources.Length,
                sources,
                response = response.Text
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini market search smoke test timed out for model {Model}", settings.MarketSearchModel);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                success = false,
                model = settings.MarketSearchModel,
                message = "Gemini Search đã vượt thời gian chờ."
            });
        }
        catch (Exception exception)
        {
            //logger.LogError(
            //    "Gemini market search smoke test failed for model {Model} with {ErrorType}",
            //    settings.MarketSearchModel,
            //    exception.GetType().Name);

            logger.LogError(
                exception,
                "Gemini market search smoke test failed for model {Model}",
                settings.MarketSearchModel);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                model = settings.MarketSearchModel,
                message = "Không gọi được Gemini Search. Kiểm tra model và quota trong log backend."
            });
        }
    }

    [HttpGet("used-price-search")]
    public async Task<IActionResult> TestUsedPriceSearch(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();

        var settings = options.Value;
        if (!settings.SearchGroundingEnabled)
            return BadRequest(new { success = false, message = "Search grounding đang bị tắt trong cấu hình." });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.ExternalUsedPriceSearchTimeoutSeconds));

        try
        {
            var response = await gemini.GenerateContentAsync(
                model: settings.MarketSearchModel,
                contents: "Dùng Google Search tìm Samsung WW90T3040WW cũ tại Việt Nam. " +
                          "Trả tên nguồn và giá tìm được.",
                config: new GenerateContentConfig
                {
                    Tools = [new Tool { GoogleSearch = new GoogleSearch() }],
                    Temperature = 0.1,
                    MaxOutputTokens = 500
                },
                cancellationToken: timeout.Token);

            var grounding = response.Candidates?.FirstOrDefault()?.GroundingMetadata;
            var sources = grounding?.GroundingChunks?
                .Where(chunk => chunk.Web != null &&
                                Uri.TryCreate(chunk.Web.Uri, UriKind.Absolute, out var uri) &&
                                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .Select(chunk => new { title = chunk.Web!.Title, url = chunk.Web.Uri })
                .DistinctBy(source => source.url)
                .ToArray() ?? [];

            logger.LogInformation(
                "Gemini used-price search diagnostic completed for model {Model} with {SourceCount} grounded sources",
                settings.MarketSearchModel,
                sources.Length);

            return Ok(new
            {
                success = !string.IsNullOrWhiteSpace(response.Text),
                model = settings.MarketSearchModel,
                grounded = sources.Length > 0,
                sourceCount = sources.Length,
                sources,
                response = response.Text
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Gemini used-price search diagnostic timed out for model {Model}",
                settings.MarketSearchModel);

            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                success = false,
                model = settings.MarketSearchModel,
                message = "Gemini Search đã vượt thời gian chờ."
            });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Gemini used-price search diagnostic failed for model {Model}",
                settings.MarketSearchModel);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                model = settings.MarketSearchModel,
                message = "Không gọi được Gemini Search. Kiểm tra model, quota và spend cap trong log backend."
            });
        }
    }

    [HttpGet("connection")]
    public Task<IActionResult> TestConnection(CancellationToken cancellationToken) =>
        TestModelConnection(options.Value.PriceSuggestionModel, cancellationToken);

    [HttpGet("connection-2.5-flash")]
    public Task<IActionResult> TestFlash25(CancellationToken cancellationToken) =>
        TestModelConnection("gemini-2.5-flash", cancellationToken);

    [HttpGet("connection-2.5-flash-lite")]
    public Task<IActionResult> TestFlashLite25(CancellationToken cancellationToken) =>
        TestModelConnection("gemini-2.5-flash-lite", cancellationToken);

    private async Task<IActionResult> TestModelConnection(string model, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();

        var settings = options.Value;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            var response = await gemini.GenerateContentAsync(
                model: model,
                contents: "Trả lời đúng một câu tiếng Việt: Gemini đã kết nối thành công.",
                config: new GenerateContentConfig
                {
                    Temperature = 0.1,
                    MaxOutputTokens = 100
                },
                cancellationToken: timeout.Token);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                logger.LogWarning("Gemini smoke test returned no text for model {Model}", model);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    model,
                    message = "Gemini không trả về văn bản."
                });
            }

            logger.LogInformation("Gemini smoke test succeeded for model {Model}", model);
            return Ok(new
            {
                success = true,
                model,
                response = response.Text
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Gemini smoke test timed out for model {Model}", model);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                success = false,
                model,
                message = "Gemini đã vượt thời gian chờ."
            });
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Gemini smoke test failed for model {Model} with {ErrorType}",
                model,
                exception.GetType().Name);
            var unavailable = exception.Message.Contains("no longer available", StringComparison.OrdinalIgnoreCase);
            var quotaExceeded = exception.Message.Contains("exceeded your current quota", StringComparison.OrdinalIgnoreCase);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                model,
                reason = unavailable ? "MODEL_UNAVAILABLE" : quotaExceeded ? "QUOTA_EXCEEDED" : "GEMINI_ERROR",
                message = unavailable
                    ? "Project này không được cấp quyền dùng model 2.5."
                    : quotaExceeded
                        ? "Đã vượt quota của model này."
                        : "Không gọi được Gemini. Kiểm tra key, model và quota trong log backend."
            });
        }
    }
}
