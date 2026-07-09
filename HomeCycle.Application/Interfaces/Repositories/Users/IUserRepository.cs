using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Users
{
    public interface IUserRepository
    {
        Task<user?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<user?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<user?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

        Task AddAsync(user user, CancellationToken cancellationToken = default);

        //token
        Task AddRefreshTokenAsync(refresh_token token, CancellationToken cancellationToken = default);
        Task<refresh_token?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
        void UpdateRefreshToken(refresh_token token);
    }
}
