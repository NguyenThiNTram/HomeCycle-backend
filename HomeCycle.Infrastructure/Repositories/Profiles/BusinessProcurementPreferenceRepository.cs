using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Profiles
{
    public class BusinessProcurementPreferenceRepository : IBusinessProcurementPreferenceRepository
    {
        private readonly HomeCycleDbContext _context;

        public BusinessProcurementPreferenceRepository(HomeCycleDbContext context)
        {
            _context = context;
        }

        public async Task<business_procurement_preference?> GetByBusinessProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken)
        {
            var persistence = await _context.Business_Procurement_Preferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.BusinessProfileId == businessProfileId, cancellationToken);
            return persistence?.ToDomain();
        }

        public async Task AddAsync(business_procurement_preference preference, CancellationToken cancellationToken)
        {
            await _context.Business_Procurement_Preferences.AddAsync(preference.ToInfrastructure(), cancellationToken);
        }

        public void Update(business_procurement_preference preference)
        {
            _context.Business_Procurement_Preferences.Update(preference.ToInfrastructure());
        }

        public async Task<bool> ExistsByBusinessProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken)
        {
            return await _context.Business_Procurement_Preferences
                .AnyAsync(p => p.BusinessProfileId == businessProfileId, cancellationToken);
        }
    }
}
