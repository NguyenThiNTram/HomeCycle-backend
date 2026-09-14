using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Entities;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Disputes
{
    public class DisputeCategoryRepository : IDisputeCategoryRepository
    {
        private readonly HomeCycleDbContext _db;

        public DisputeCategoryRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<dispute_category>> GetAllAsync(bool? isActive = null, CancellationToken ct = default)
        {
            var query = _db.Dispute_Categories.AsNoTracking().Include(x => x.Targets).AsQueryable();

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            var entities = await query.OrderBy(x => x.DisputeCategoryId).ToListAsync(ct);
            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<IReadOnlyList<dispute_category>> GetActiveByTargetTypeAsync(
            DisputeTargetType targetType, CancellationToken ct = default)
        {
            var target = (int)targetType;

            var entities = await _db.Dispute_Categories
                .AsNoTracking()
                .Include(x => x.Targets)
                .Where(x => x.IsActive && x.Targets.Any(t => t.TargetType == target))
                .OrderBy(x => x.DisputeCategoryId)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<dispute_category?> GetByIdAsync(int categoryId, CancellationToken ct = default)
        {
            var entity = await _db.Dispute_Categories
                .AsNoTracking()
                .Include(x => x.Targets)
                .SingleOrDefaultAsync(x => x.DisputeCategoryId == categoryId, ct);

            return entity?.ToDomain();
        }

        public async Task<dispute_category?> GetByIdForUpdateAsync(int categoryId, CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("FOR UPDATE requires an active database transaction.");

            var entity = await _db.Dispute_Categories
                .FromSqlInterpolated($@"SELECT * FROM public.""Dispute_Category"" WHERE ""DisputeCategoryId"" = {categoryId} FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            if (entity == null)
                return null;

            var category = entity.ToDomain();
            category.TargetTypes = await _db.Dispute_Category_Targets
                .AsNoTracking()
                .Where(x => x.DisputeCategoryId == categoryId)
                .Select(x => (DisputeTargetType)x.TargetType)
                .ToListAsync(ct);

            return category;
        }

        public async Task<dispute_category?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            var entity = await _db.Dispute_Categories
                .AsNoTracking()
                .Include(x => x.Targets)
                .SingleOrDefaultAsync(x => x.Code == code, ct);

            return entity?.ToDomain();
        }

        public Task<bool> ExistsCodeAsync(string code, CancellationToken ct = default)
            => _db.Dispute_Categories.AsNoTracking().AnyAsync(x => x.Code == code, ct);

        public async Task AddAsync(dispute_category category, CancellationToken ct = default)
        {
            var entity = category.ToInfrastructure();

            foreach (var targetType in category.TargetTypes.Distinct())
                entity.Targets.Add(new Dispute_Category_Target { TargetType = (int)targetType });

            await _db.Dispute_Categories.AddAsync(entity, ct);
        }

        public Task UpdateAsync(dispute_category category, CancellationToken ct = default)
        {
            _db.Dispute_Categories.Update(category.ToInfrastructure());
            return Task.CompletedTask;
        }

        public async Task ReplaceTargetsAsync(
            int categoryId, IReadOnlyCollection<DisputeTargetType> targetTypes, CancellationToken ct = default)
        {
            await _db.Dispute_Category_Targets
                .Where(x => x.DisputeCategoryId == categoryId)
                .ExecuteDeleteAsync(ct);

            var targets = targetTypes.Distinct()
                .Select(x => new Dispute_Category_Target
                {
                    DisputeCategoryId = categoryId,
                    TargetType = (int)x
                });

            await _db.Dispute_Category_Targets.AddRangeAsync(targets, ct);
        }
    }
}
