using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages
{
    public interface IUserSubscriptionRepository
    {
        Task<user_subscription?> GetActiveWithEntitlementsAsync(Guid userId, DateTime atUtc, CancellationToken cancellationToken = default);
        Task<user_subscription?> GetOpenAsync(Guid userId, DateTime atUtc, CancellationToken cancellationToken = default);
        Task<user_subscription?> GetOpenForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<user_subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
        Task<user_subscription?> GetByIdForUpdateAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
        Task AddAsync(user_subscription subscription, CancellationToken cancellationToken = default);
        Task UpdateAsync(user_subscription subscription, CancellationToken cancellationToken = default);
        Task ReplaceEntitlementsAsync(Guid subscriptionId, IReadOnlyCollection<user_subscription_entitlement> entitlements, CancellationToken cancellationToken = default);
    }
}
