using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchEntitlementService
{
    Task<SupplierMatchEntitlement> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetActiveVipUserIdsAsync(
        CancellationToken cancellationToken = default);
}
