using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.Externals.Gemini;

public sealed class GeminiRequestService(
    Client client,
    IOptions<GeminiOptions> options,
    IMemoryCache cache,
    TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, ModelGate> _modelGates = new();

    public async Task<GenerateContentResponse> GenerateContentAsync(
        string model,
        string contents,
        GenerateContentConfig config,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.SearchGroundingEnabled &&
            config.Tools?.Any(tool => tool.GoogleSearch != null) == true)
            throw new InvalidOperationException("Google Search grounding đang bị tắt trong cấu hình Gemini.");

        var key = CreateCacheKey(model, contents, config);
        if (settings.ResponseCacheMinutes > 0 && cache.TryGetValue(key, out GenerateContentResponse? cached) && cached != null)
            return cached;

        var gate = _modelGates.GetOrAdd(model, _ => new ModelGate());
        await gate.Semaphore.WaitAsync(cancellationToken);
        try
        {
            // Simultaneous identical requests share one successful response.
            if (settings.ResponseCacheMinutes > 0 && cache.TryGetValue(key, out cached) && cached != null)
                return cached;

            var interval = TimeSpan.FromMinutes(1.0 / Math.Max(1, settings.RequestsPerMinute));
            var delay = gate.NextStart - clock.GetUtcNow();
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            gate.NextStart = clock.GetUtcNow() + interval;
            var response = await client.Models.GenerateContentAsync(
                model: model,
                contents: contents,
                config: config,
                cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Text) && settings.ResponseCacheMinutes > 0)
                cache.Set(key, response, TimeSpan.FromMinutes(settings.ResponseCacheMinutes));

            return response;
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    private static string CreateCacheKey(string model, string contents, GenerateContentConfig config)
    {
        var bytes = Encoding.UTF8.GetBytes(model + "\n" + contents + "\n" + JsonSerializer.Serialize(config));
        return "gemini:" + Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed class ModelGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public DateTimeOffset NextStart { get; set; }
    }
}
