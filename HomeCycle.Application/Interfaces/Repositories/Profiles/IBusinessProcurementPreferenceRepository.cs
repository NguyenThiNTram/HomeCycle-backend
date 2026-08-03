using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Profiles
{
    public interface IBusinessProcurementPreferenceRepository
    {
        Task<business_procurement_preference?> GetByBusinessProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken);
        Task AddAsync(business_procurement_preference preference, CancellationToken cancellationToken);
        void Update(business_procurement_preference preference);
        Task<bool> ExistsByBusinessProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken);
    }
}
