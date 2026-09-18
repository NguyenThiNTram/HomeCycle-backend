using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Repositories.SupplierMatching;

public interface ISupplierMatchMonitorRepository
{
    Task DisableClosedOrExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierMatchMonitorTarget>> GetTargetsAsync(
        IReadOnlyCollection<Guid> vipUserIds,
        int batchSize,
        CancellationToken cancellationToken = default);
    Task<SupplierMatchMonitorStateSnapshot?> GetStateAsync(Guid buyPostId, CancellationToken cancellationToken = default);
    Task InitializeAsync(Guid buyPostId, Guid userId, decimal bestScore,
        IReadOnlyList<SupplierMatchMonitorCandidate> candidates, DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task<bool> TryClaimImprovementAsync(Guid buyPostId, Guid sellPostId, decimal score,
        decimal minimumDelta, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(Guid buyPostId, Guid userId, decimal bestScore, DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task DisableAsync(Guid buyPostId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
