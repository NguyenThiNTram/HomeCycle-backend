using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchQuota
{
    Task<SupplierMatchQuotaStatus> GetRemainingAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<SupplierMatchQuotaReservation> TryReserveAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
