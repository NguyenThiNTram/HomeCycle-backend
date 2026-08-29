using HomeCycle.Application.Interfaces.Repositories.PlatformPolicies;
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

namespace HomeCycle.Infrastructure.Repositories.PlatformPolicies
{
    public class PlatformPolicyRepository : IPlatformPolicyRepository
    {
        private readonly HomeCycleDbContext _db;

        public PlatformPolicyRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<platform_policy?> GetActiveAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default)
        {
            var type = policyType.ToString();

            var entity = await _db.Platform_Policies
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.PolicyType == type && x.IsActive, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<platform_policy?> GetActiveForUpdateAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default)
        {
            var type = policyType.ToString();

            var entity = await _db.Platform_Policies
                .FromSqlInterpolated($@"SELECT * FROM ""Platform_Policy"" WHERE ""PolicyType"" = {type} AND ""IsActive"" = TRUE FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<int> GetNextVersionAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default)
        {
            var type = policyType.ToString();

            var maxVersion = await _db.Platform_Policies
                .AsNoTracking()
                .Where(x => x.PolicyType == type)
                .MaxAsync(x => (int?)x.Version, cancellationToken);

            return (maxVersion ?? 0) + 1;
        }

        public async Task AddAsync(platform_policy policy, CancellationToken cancellationToken = default)
        {
            await _db.Platform_Policies.AddAsync(policy.ToInfrastructure(), cancellationToken);
        }

        public Task UpdateAsync(platform_policy policy, CancellationToken cancellationToken = default)
        {
            _db.Platform_Policies.Update(policy.ToInfrastructure());
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<platform_policy>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _db.Platform_Policies
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.PolicyType)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<IReadOnlyList<platform_policy>> GetVersionsAsync(
            PlatformPolicyType policyType,
            CancellationToken cancellationToken = default)
        {
            var type = policyType.ToString();

            var entities = await _db.Platform_Policies
                .AsNoTracking()
                .Where(x => x.PolicyType == type)
                .OrderByDescending(x => x.Version)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<platform_policy?> GetByVersionAsync(
            PlatformPolicyType policyType,
            int version,
            CancellationToken cancellationToken = default)
        {
            var type = policyType.ToString();

            var entity = await _db.Platform_Policies
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.PolicyType == type && x.Version == version,
                    cancellationToken);

            return entity?.ToDomain();
        }
    }
}
