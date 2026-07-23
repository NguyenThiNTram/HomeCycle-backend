using Google;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Users
{
    public class BusinessProfileRepository : IBusinessProfileRepository
    {
        private readonly HomeCycleDbContext _db;

        public BusinessProfileRepository(HomeCycleDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<business_profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Business_Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);
            
            return entity?.ToDomain();
        }
    }
}
