using HomeCycle.Application.Interfaces.Repositories.Media;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Posts
{
    public class MediaRepository : IMediaRepository
    {
        private readonly HomeCycleDbContext _db;

        public MediaRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddRangeAsync(IEnumerable<media> entities, CancellationToken cancellationToken = default)
        {
            var infraEntities = entities.Select(x => x.ToInfrastructure());
            await _db.Media.AddRangeAsync(infraEntities, cancellationToken);
        }

        public async Task<IReadOnlyList<media>> GetByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default)
        {
            return await _db.Media
                .AsNoTracking()
                .Where(x => x.TargetId == targetId && x.TargetType == targetType)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ToDomain())
                .ToListAsync(cancellationToken);
        }

        public async Task RemoveByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default)
        {
            var items = await _db.Media
                .Where(x => x.TargetId == targetId && x.TargetType == targetType)
                .ToListAsync(cancellationToken);

            if (items.Count > 0)
                _db.Media.RemoveRange(items);
        }
    }
}
