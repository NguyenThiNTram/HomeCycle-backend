using HomeCycle.Application.Interfaces.Security;
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

        public async Task AddModeratorTokenAsync(otp token, CancellationToken cancellationToken = default)
        {
            await _db.OTPs.AddAsync(token.ToInfrastructure(), cancellationToken);
        }

        public async Task<otp?> GetModeratorTokenAsync(string hash, string purpose, CancellationToken cancellationToken = default)
        {
            var entity = await _db.OTPs.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Code == hash && x.Purpose == purpose && !x.IsUsed &&
                x.ExpiredAt > DateTime.UtcNow, cancellationToken);
            return entity == null ? null : entity.ToDomain();
        }

        public async Task<bool> ConsumeModeratorTokenAsync(Guid tokenId, string purpose, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _db.OTPs.Where(x => x.OtpId == tokenId && x.Purpose == purpose &&
                    !x.IsUsed && x.ExpiredAt > now)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsUsed, true)
                    .SetProperty(x => x.UsedAt, (DateTime?)now), cancellationToken) == 1;
        }

        public async Task<bool> UpdateModeratorActivationAsync(Guid userId, string? passwordHash, CancellationToken cancellationToken = default)
        {
            var query = _db.Users.Where(x => x.UserId == userId &&
                x.Role == (int)HomeCycle.Domain.Enums.UserRole.Moderator &&
                x.Status == (int)HomeCycle.Domain.Enums.UserStatus.Pending);
            if (passwordHash == null)
                return await query.Where(x => !x.IsEmailVerified)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsEmailVerified, true), cancellationToken) == 1;

            return await query.Where(x => x.IsEmailVerified)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Password, passwordHash)
                    .SetProperty(x => x.Status, (int)HomeCycle.Domain.Enums.UserStatus.Active), cancellationToken) == 1;
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
                    x.Purpose == "Register" &&
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

        public async Task UpdateUserIdAsync(string email, Guid userId, CancellationToken cancellationToken)
        {
            var otp = await _db.OTPs
                .Where(x => x.Email == email && x.Purpose == "Register")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
                return;

            otp.UserId = userId;
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
