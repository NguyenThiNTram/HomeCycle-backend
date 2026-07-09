using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
