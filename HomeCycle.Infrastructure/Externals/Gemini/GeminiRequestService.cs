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

        var gate = GetModelGate(model);
        await gate.TextSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Simultaneous identical requests share one successful response.
            if (settings.ResponseCacheMinutes > 0 && cache.TryGetValue(key, out cached) && cached != null)
                return cached;

            await WaitForNextStartAsync(gate, settings.RequestsPerMinute, cancellationToken);
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
            gate.TextSemaphore.Release();
        }
    }

    public async Task<GenerateContentResponse> GenerateContentWithImagesAsync(
        string model,
        string prompt,
        IReadOnlyList<(byte[] Data, string MimeType)> images,
        GenerateContentConfig config,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<Part> { Part.FromText(prompt) };
        parts.AddRange(images.Select(image => Part.FromBytes(image.Data, image.MimeType, null)));

        var settings = options.Value;
        var gate = GetModelGate(model);
        await gate.ScanSemaphore.WaitAsync(cancellationToken);
        try
        {
            await WaitForNextStartAsync(gate, settings.RequestsPerMinute, cancellationToken);
            return await client.Models.GenerateContentAsync(
                model: model,
                contents: new Content { Role = "user", Parts = parts },
                config: config,
                cancellationToken: cancellationToken);
        }
        finally
        {
            gate.ScanSemaphore.Release();
        }
    }

    private ModelGate GetModelGate(string model) => _modelGates.GetOrAdd(
        model,
        _ => new ModelGate(Math.Max(1, options.Value.IdentityDocumentScanMaxConcurrency)));

    private async Task WaitForNextStartAsync(ModelGate gate, int requestsPerMinute, CancellationToken cancellationToken)
    {
        await gate.StartSlotSemaphore.WaitAsync(cancellationToken);
        TimeSpan delay;
        try
        {
            var now = clock.GetUtcNow();
            var scheduledStart = gate.NextStart > now ? gate.NextStart : now;
            gate.NextStart = scheduledStart + TimeSpan.FromMinutes(1.0 / Math.Max(1, requestsPerMinute));
            delay = scheduledStart - now;
        }
        finally
        {
            gate.StartSlotSemaphore.Release();
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);
    }

    private static string CreateCacheKey(string model, string contents, GenerateContentConfig config)
    {
        var bytes = Encoding.UTF8.GetBytes(model + "\n" + contents + "\n" + JsonSerializer.Serialize(config));
        return "gemini:" + Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed class ModelGate
    {
        public ModelGate(int scanConcurrencyLimit)
        {
            ScanSemaphore = new SemaphoreSlim(scanConcurrencyLimit, scanConcurrencyLimit);
        }

        public SemaphoreSlim TextSemaphore { get; } = new(1, 1);
        public SemaphoreSlim ScanSemaphore { get; }
        public SemaphoreSlim StartSlotSemaphore { get; } = new(1, 1);
        public DateTimeOffset NextStart { get; set; }
    }
}
