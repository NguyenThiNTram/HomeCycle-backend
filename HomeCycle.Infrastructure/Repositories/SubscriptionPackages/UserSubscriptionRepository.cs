using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.SubscriptionPackages
{
    public class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly HomeCycleDbContext _db;

        public UserSubscriptionRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<user_subscription?> GetActiveWithEntitlementsAsync(
            Guid userId,
            DateTime atUtc,
            CancellationToken cancellationToken = default)
        {
            var subscriptions = await _db.User_Subscriptions
                .AsNoTracking()
                .Include(x => x.User_Subscription_Entitlements)
                .Where(x =>
                    x.UserId == userId &&
                    x.Status == (int)UserSubscriptionStatus.Active &&
                    x.ActivatedAt.HasValue &&
                    x.ActivatedAt.Value <= atUtc &&
                    x.ExpiresAt.HasValue &&
                    x.ExpiresAt.Value > atUtc)
                .OrderByDescending(x => x.ActivatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count > 1)
            {
                throw new InvalidOperationException(
                    "Expected at most one active subscription for the user.");
            }

            return subscriptions.SingleOrDefault()?.ToDomain();
        }
    }
}
