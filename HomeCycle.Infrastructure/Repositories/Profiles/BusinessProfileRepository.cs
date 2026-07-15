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
    }
}
