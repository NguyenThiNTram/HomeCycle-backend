using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals
{
    public class OtpRepository : IOtpRepository
    {
        private readonly HomeCycleDbContext _db;

        public OtpRepository(HomeCycleDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task AddAsync(otp otp)
        {
            var entity = otp.ToInfrastructure();

            await _db.OTPs.AddAsync(entity!);
            await _db.SaveChangesAsync();
        }

        public async Task<otp?> GetValidOtpAsync(string email, string code)
        {
            var entity = await _db.OTPs
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.Code == code &&
                    !x.IsUsed &&
                    x.ExpiredAt > DateTime.UtcNow);

            return entity.ToDomain();
        }

        public async Task UpdateAsync(otp otp)
        {
            var entity = await _db.OTPs.FirstOrDefaultAsync(x => x.OtpId == otp.OtpId);


            if (entity == null)
                return;

            entity.IsUsed = otp.IsUsed;
            entity.UsedAt = otp.UsedAt;

            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.OTPs
                .AnyAsync(x =>
                    x.Email == email &&
                    x.Purpose == "Register" &&
                    x.IsUsed &&
                    x.UsedAt != null &&
                    x.ExpiredAt > DateTime.UtcNow
                    , cancellationToken
                );
        }
    }
}
