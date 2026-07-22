using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Profiles
{
    public class BusinessProfileRepository : IBusinessProfileRepository
    {
        private readonly HomeCycleDbContext _db;

        public BusinessProfileRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<business_profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Business_Profiles
                .AsNoTracking() // Read-only query tối ưu hiệu năng và bộ nhớ
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            return entity.ToDomain();
        }

        public async Task<business_profile?> GetByIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Business_Profiles
                .AsNoTracking() // Tối ưu bộ nhớ & tránh Change Tracker giữ Instance
                .FirstOrDefaultAsync(x => x.BusinessProfileId == businessProfileId, cancellationToken);

            return entity?.ToDomain();
        }
        public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Business_Profiles
                .AnyAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(business_profile profile, CancellationToken cancellationToken = default)
        {
            var entity = profile.ToInfrastructure();
            await _db.Business_Profiles.AddAsync(entity, cancellationToken);
        }

        public void Update(business_profile profile)
        {
            var entity = profile.ToInfrastructure();
            _db.Business_Profiles.Update(entity);
        }

        public async Task<List<business_profile>> GetPendingProfilesAsync(
            string? keyword,
            CancellationToken cancellationToken = default)
        {
            const int PendingStatus = 0; 

            // Query đọc nhanh, không theo dõi tracking state
            var query = _db.Business_Profiles
                .AsNoTracking()
                .Where(b => b.Status == PendingStatus);

            // Xử lý tìm kiếm động nếu người dùng nhập keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var cleanKeyword = keyword.Trim().ToLower();
                query = query.Where(b =>
                    (b.BusinessName != null && b.BusinessName.ToLower().Contains(cleanKeyword)) ||
                    (b.FullName != null && b.FullName.ToLower().Contains(cleanKeyword)) ||
                    (b.TaxCode != null && b.TaxCode.ToLower().Contains(cleanKeyword))
                );
            }


            var entities = await query
                .OrderBy(b => b.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            return entities.Select(e => e.ToDomain()!).ToList();
        }
    }
}
