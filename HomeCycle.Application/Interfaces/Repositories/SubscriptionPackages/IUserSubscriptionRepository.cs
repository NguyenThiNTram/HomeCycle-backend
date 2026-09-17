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
        Task<user_subscription?> GetActiveWithEntitlementsAsync(
            Guid userId,
            DateTime atUtc,
            CancellationToken cancellationToken = default);
    }
}
