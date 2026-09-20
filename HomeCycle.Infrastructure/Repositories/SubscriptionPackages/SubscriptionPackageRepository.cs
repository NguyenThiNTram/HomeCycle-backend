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
    public class SubscriptionPackageRepository : ISubscriptionPackageRepository
    {
        private readonly HomeCycleDbContext _db;

        public Task<bool> HasSubscriptionsAsync(Guid packageId, CancellationToken cancellationToken = default)
            => _db.User_Subscriptions.AnyAsync(x => x.PackageId == packageId, cancellationToken);

        public async Task DeleteAsync(Guid packageId, CancellationToken cancellationToken = default)
        {
            await _db.Subscription_Package_Entitlements.Where(x => x.PackageId == packageId).ExecuteDeleteAsync(cancellationToken);
            await _db.Subscription_Packages.Where(x => x.PackageId == packageId).ExecuteDeleteAsync(cancellationToken);
        }

        public SubscriptionPackageRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<subscription_package>> GetAllAsync(
            bool? isActive,
            UserRole? targetRole,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Subscription_Packages
                .AsNoTracking()
                .Include(x => x.Subscription_Package_Entitlements)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            if (targetRole.HasValue)
                query = query.Where(x => x.TargetRole == (int)targetRole.Value);

            var entities = await query
                .OrderBy(x => x.Price)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<subscription_package?> GetByIdAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Subscription_Packages
                .AsNoTracking()
                .Include(x => x.Subscription_Package_Entitlements)
                .SingleOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<subscription_package?> GetByIdForUpdateAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            if (_db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("FOR UPDATE requires an active database transaction.");

            var entity = await _db.Subscription_Packages
                .FromSqlInterpolated($@"SELECT * FROM public.""Subscription_Package"" WHERE ""PackageId"" = {packageId} FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            if (entity == null)
                return null;

            entity.Subscription_Package_Entitlements = await _db.Subscription_Package_Entitlements
                .AsNoTracking()
                .Where(x => x.PackageId == packageId)
                .OrderBy(x => x.EntitlementKey)
                .ToListAsync(cancellationToken);

            return entity.ToDomain();
        }

        public Task<bool> ExistsByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return _db.Subscription_Packages
                .AsNoTracking()
                .AnyAsync(x => x.Code == code, cancellationToken);
        }

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludedPackageId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Subscription_Packages
                .AsNoTracking()
                .Where(x => EF.Functions.ILike(x.Name, name.Trim()));

            if (excludedPackageId.HasValue)
                query = query.Where(x => x.PackageId != excludedPackageId.Value);

            return query.AnyAsync(cancellationToken);
        }

        public async Task AddAsync(
            subscription_package package,
            CancellationToken cancellationToken = default)
        {
            await _db.Subscription_Packages.AddAsync(
                package.ToInfrastructure(),
                cancellationToken);
        }

        public Task UpdateAsync(
            subscription_package package,
            CancellationToken cancellationToken = default)
        {
            _db.Subscription_Packages.Update(package.ToInfrastructure(false));
            return Task.CompletedTask;
        }

        public async Task ReplaceEntitlementsAsync(
            Guid packageId,
            IReadOnlyCollection<subscription_package_entitlement> entitlements,
            CancellationToken cancellationToken = default)
        {
            await _db.Subscription_Package_Entitlements
                .Where(x => x.PackageId == packageId)
                .ExecuteDeleteAsync(cancellationToken);

            var entities = entitlements.Select(x =>
            {
                x.PackageId = packageId;
                return x.ToInfrastructure();
            });

            await _db.Subscription_Package_Entitlements.AddRangeAsync(
                entities,
                cancellationToken);
        }
    }
}
