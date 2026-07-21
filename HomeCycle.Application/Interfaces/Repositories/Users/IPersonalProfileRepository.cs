using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Users
{
    public interface IPersonalProfileRepository
    {
        Task<personal_profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(personal_profile profile, CancellationToken cancellationToken = default);

        Task UpdateAsync(personal_profile profile, CancellationToken cancellationToken = default);

        Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
