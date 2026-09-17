using System.Collections.Concurrent;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class MemorySupplierMatchCache(
    IMemoryCache cache,
    IOptions<SupplierMatchingOptions> options) : ISupplierMatchCache
{
    private readonly ConcurrentDictionary<string, LockState> _locks = new(StringComparer.Ordinal);

    public bool TryGet(string fingerprint, out SupplierMatchCacheEntry? entry) =>
        cache.TryGetValue(CacheKey(fingerprint), out entry);

    public void Set(string fingerprint, SupplierMatchCacheEntry entry) =>
        cache.Set(CacheKey(fingerprint), entry,
            TimeSpan.FromMinutes(Math.Clamp(options.Value.CacheMinutes, 1, 1440)));

    public void Remove(string fingerprint) => cache.Remove(CacheKey(fingerprint));

    public async ValueTask<IAsyncDisposable> AcquireAsync(
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        var state = _locks.GetOrAdd(fingerprint, _ => new LockState());
        Interlocked.Increment(ref state.ReferenceCount);
        try
        {
            await state.Semaphore.WaitAsync(cancellationToken);
            return new Lease(this, fingerprint, state);
        }
        catch
        {
            ReleaseReference(fingerprint, state, releaseSemaphore: false);
            throw;
        }
    }

    private void ReleaseReference(string key, LockState state, bool releaseSemaphore)
    {
        if (releaseSemaphore) state.Semaphore.Release();
        if (Interlocked.Decrement(ref state.ReferenceCount) == 0)
            _locks.TryRemove(new KeyValuePair<string, LockState>(key, state));
    }

    private static string CacheKey(string fingerprint) => "supplier-match:" + fingerprint;

    private sealed class LockState
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount;
    }

    private sealed class Lease(MemorySupplierMatchCache owner, string key, LockState state) : IAsyncDisposable
    {
        private int _disposed;
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.ReleaseReference(key, state, releaseSemaphore: true);
            return ValueTask.CompletedTask;
        }
    }
}
