using AutoMapper;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Users
{
    public class PersonalProfileRepository : IPersonalProfileRepository
    {
        private readonly HomeCycleDbContext _db;

        public PersonalProfileRepository(HomeCycleDbContext db)
        {
            _db = db;
        }


        public async Task AddAsync(personal_profile profile, CancellationToken cancellationToken = default)
        {
            var entity = profile.ToInfrastructure();

            await _db.Personal_Profiles.AddAsync(entity, cancellationToken);
        }

        public async Task<personal_profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Personal_Profiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task UpdateAsync(personal_profile profile, CancellationToken cancellationToken = default)
        {
            var entity = profile.ToInfrastructure();
            var localEntry = _db.Personal_Profiles.Local.FirstOrDefault(x => x.PersonalProfileId == entity.PersonalProfileId);

            if (localEntry != null)
            {
                // Trục xuất thực thể cũ ra khỏi Change Tracker để nhường chỗ cho thực thể cập nhật
                _db.Entry(localEntry).State = EntityState.Detached;
            }

            _db.Personal_Profiles.Update(entity);
            //await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsByUserIdAsync( Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Personal_Profiles.AnyAsync(x => x.UserId == userId, cancellationToken);
        }
    }
}
