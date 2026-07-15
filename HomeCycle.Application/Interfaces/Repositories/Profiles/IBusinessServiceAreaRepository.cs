using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Profiles
{
    public interface IBusinessServiceAreaRepository
    {
        Task AddAsync(business_service_area serviceArea, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<business_service_area> serviceAreas, CancellationToken cancellationToken = default);
        Task<List<business_service_area>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
        Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
    }
}
