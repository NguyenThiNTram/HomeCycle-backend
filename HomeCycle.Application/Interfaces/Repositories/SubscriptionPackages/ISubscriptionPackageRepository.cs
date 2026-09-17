using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages
{
    public interface ISubscriptionPackageRepository
    {
        Task<IReadOnlyList<subscription_package>> GetAllAsync(bool? isActive, UserRole? targetRole, CancellationToken cancellationToken = default);
        Task<subscription_package?> GetByIdAsync(Guid packageId, CancellationToken cancellationToken = default);
        Task<subscription_package?> GetByIdForUpdateAsync(Guid packageId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, Guid? excludedPackageId = null, CancellationToken cancellationToken = default);
        Task AddAsync(subscription_package package, CancellationToken cancellationToken = default);
        Task UpdateAsync(subscription_package package, CancellationToken cancellationToken = default);
        Task ReplaceEntitlementsAsync(Guid packageId, IReadOnlyCollection<subscription_package_entitlement> entitlements, CancellationToken cancellationToken = default);
    }
}
