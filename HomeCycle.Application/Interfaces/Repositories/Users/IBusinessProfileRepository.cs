using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Users
{
    public interface IBusinessProfileRepository
    {
        Task<business_profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
