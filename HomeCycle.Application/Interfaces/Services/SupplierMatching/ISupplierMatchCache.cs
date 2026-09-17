using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchCache
{
    bool TryGet(string fingerprint, out SupplierMatchCacheEntry? entry);
    void Set(string fingerprint, SupplierMatchCacheEntry entry);
    void Remove(string fingerprint);
    ValueTask<IAsyncDisposable> AcquireAsync(
        string fingerprint,
        CancellationToken cancellationToken = default);
}
