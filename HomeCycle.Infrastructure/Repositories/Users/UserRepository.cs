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
    public class UserRepository : IUserRepository
    {
        private readonly HomeCycleDbContext _db;

        public UserRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        //cái này cho user hệ thống, personal và business phải add ở repo riêng
        public async Task AddAsync(user user, CancellationToken cancellationToken = default)
        {
            var entity = user.ToInfrastructure();

            await _db.Users.AddAsync(entity, cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.Users
               .AnyAsync(
                   x => x.Email.ToLower() == email.ToLower(),
                   cancellationToken);
        }

        public async Task<user?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken
                );

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _db.Users
               .AnyAsync(
                   x => x.Username.ToLower() == username.ToLower(),
                   cancellationToken);
        }

        public async Task<user?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Users
               .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<user?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Users
               .FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower(), cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddRefreshTokenAsync(refresh_token token, CancellationToken cancellationToken = default)
        {
            await _db.Refresh_Tokens.AddAsync(token.ToInfrastructure(), cancellationToken);
        }

        public async Task<refresh_token?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Refresh_Tokens
            .FirstOrDefaultAsync(
                x => x.Token == token,
                cancellationToken);

            return entity?.ToDomain();
        }

        public void UpdateRefreshToken(refresh_token token)
        {
            _db.Refresh_Tokens.Update(token.ToInfrastructure());
        }

        public async Task UpdateAsync(user user, CancellationToken cancellationToken = default)
        {
            var entity = user.ToInfrastructure();
            _db.Users.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
