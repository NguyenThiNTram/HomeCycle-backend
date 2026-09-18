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

        public async Task<user_subscription?> GetActiveWithEntitlementsAsync(Guid userId, DateTime atUtc, CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException("Expected at most one active subscription for the user.");

            return subscriptions.SingleOrDefault()?.ToDomain();
        }

        public async Task<user_subscription?> GetOpenAsync(Guid userId, DateTime atUtc, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _db.User_Subscriptions
                .AsNoTracking()
                .Include(x => x.User_Subscription_Entitlements)
                .Where(x =>
                    x.UserId == userId &&
                    (x.Status == (int)UserSubscriptionStatus.Pending ||
                     x.Status == (int)UserSubscriptionStatus.Active &&
                     x.ExpiresAt.HasValue &&
                     x.ExpiresAt.Value > atUtc))
                .OrderByDescending(x => x.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count > 1)
                throw new InvalidOperationException("Expected at most one open subscription for the user.");

            return subscriptions.SingleOrDefault()?.ToDomain();
        }

        public async Task<user_subscription?> GetOpenForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (_db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("FOR UPDATE requires an active database transaction.");

            var pending = (int)UserSubscriptionStatus.Pending;
            var active = (int)UserSubscriptionStatus.Active;

            var entities = await _db.User_Subscriptions
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM public.""User_Subscription""
                    WHERE ""UserId"" = {userId}
                      AND ""Status"" IN ({pending}, {active})
                    ORDER BY ""CreatedAt"" DESC
                    LIMIT 2
                    FOR UPDATE")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (entities.Count > 1)
                throw new InvalidOperationException("Expected at most one open subscription for the user.");

            return entities.SingleOrDefault()?.ToDomain();
        }

        public async Task<user_subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.User_Subscriptions
                .AsNoTracking()
                .Include(x => x.User_Subscription_Entitlements)
                .SingleOrDefaultAsync(x => x.SubscriptionId == subscriptionId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<user_subscription?> GetByIdForUpdateAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            if (_db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("FOR UPDATE requires an active database transaction.");

            var entity = await _db.User_Subscriptions
                .FromSqlInterpolated($@"SELECT * FROM public.""User_Subscription"" WHERE ""SubscriptionId"" = {subscriptionId} FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            if (entity != null)
            {
                entity.User_Subscription_Entitlements = await _db.User_Subscription_Entitlements
                    .AsNoTracking()
                    .Where(x => x.SubscriptionId == subscriptionId)
                    .ToListAsync(cancellationToken);
            }

            return entity?.ToDomain();
        }

        public async Task AddAsync(user_subscription subscription, CancellationToken cancellationToken = default)
        {
            await _db.User_Subscriptions.AddAsync(subscription.ToInfrastructure(), cancellationToken);
        }

        public Task UpdateAsync(user_subscription subscription, CancellationToken cancellationToken = default)
        {
            var entity = subscription.ToInfrastructure(false);
            var tracked = _db.User_Subscriptions.Local.SingleOrDefault(x => x.SubscriptionId == subscription.SubscriptionId);
            if (tracked != null)
                _db.Entry(tracked).CurrentValues.SetValues(entity);
            else
                _db.User_Subscriptions.Update(entity);
            return Task.CompletedTask;
        }

        public async Task ReplaceEntitlementsAsync(Guid subscriptionId, IReadOnlyCollection<user_subscription_entitlement> entitlements, CancellationToken cancellationToken = default)
        {
            await _db.User_Subscription_Entitlements
                .Where(x => x.SubscriptionId == subscriptionId)
                .ExecuteDeleteAsync(cancellationToken);

            await _db.User_Subscription_Entitlements.AddRangeAsync(
                entitlements.Select(x => x.ToInfrastructure()),
                cancellationToken);
        }
    }
}
