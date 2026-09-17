using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Repositories.SupplierMatching;

public interface ISupplierMatchCandidateRepository
{
    Task<IReadOnlyList<SupplierCandidate>> GetCandidatesAsync(
        SupplierDemandContext demand,
        int candidateLimit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierCandidate>> GetCandidatesByIdsAsync(
        SupplierDemandContext demand,
        IReadOnlyCollection<Guid> sellPostIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierCandidateLiveState>> GetLiveStatesAsync(
        IReadOnlyCollection<Guid> sellPostIds,
        CancellationToken cancellationToken = default);
}
