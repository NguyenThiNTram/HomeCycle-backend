namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchMonitorTarget(Guid BuyPostId, Guid UserId);

public sealed record SupplierMatchMonitorStateSnapshot(
    Guid BuyPostId,
    Guid UserId,
    decimal LastBestScore,
    DateTimeOffset LastCheckedAt,
    bool IsActive);

public sealed record SupplierMatchMonitorCandidate(Guid SellPostId, decimal Score);
