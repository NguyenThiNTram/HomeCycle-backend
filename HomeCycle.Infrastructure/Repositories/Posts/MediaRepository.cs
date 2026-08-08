using HomeCycle.Application.Interfaces.Repositories.Media;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;
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
            //return await _db.Media
            //    .AsNoTracking()
            //    .Where(x =>
            //        x.TargetId == targetId &&
            //        x.TargetType == targetType)
            //    .OrderBy(x => x.DisplayOrder)
            //    .Select(x => x.ToDomain())
            //    .ToListAsync(cancellationToken);

            if (targetId == Guid.Empty)
                return Array.Empty<media>();

            var entities = await _db.Media
                .AsNoTracking()
                .Where(x =>
                    x.TargetId == targetId &&
                    x.TargetType == targetType)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            return entities
                .Select(x => x.ToDomain())
                .ToList();
        }

        public async Task<IReadOnlyList<media>> GetByTargetsAsync(IReadOnlyCollection<Guid> targetId, string targetType, CancellationToken cancellationToken = default)
        {
            if (targetId.Count == 0)
                return Array.Empty<media>();

            var ids = targetId
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

            var entities = await _db.Media
                .AsNoTracking()
                .Where(x =>
                    x.TargetId.HasValue &&
                    ids.Contains(x.TargetId.Value) &&
                    x.TargetType == targetType)
                .OrderBy(x => x.TargetId)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            return entities
                .Select(x => x.ToDomain())
                .ToList();
        }

        public async Task<IReadOnlyList<media>> RemoveByTargetAsync(
            Guid targetId,
            string targetType,
            CancellationToken cancellationToken = default)
        {
            var items = await _db.Media
                .Where(x => x.TargetId == targetId && x.TargetType == targetType)
                .ToListAsync(cancellationToken);

            var oldMedias = items
                .Select(x => x.ToDomain())
                .ToList();

            _db.Media.RemoveRange(items);

            return oldMedias;
        }
    }
}
