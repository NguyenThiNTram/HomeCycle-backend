using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchAiReranker
{
    Task<SupplierMatchAiResult?> RerankAsync(
        SupplierDemandContext demand,
        IReadOnlyList<SupplierMatchAiCandidate> candidates,
        CancellationToken cancellationToken = default);
}
