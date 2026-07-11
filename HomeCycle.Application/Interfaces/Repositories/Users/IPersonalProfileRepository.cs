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
        Task AddAsync(personal_profile profile, CancellationToken cancellationToken = default);
    }
}
