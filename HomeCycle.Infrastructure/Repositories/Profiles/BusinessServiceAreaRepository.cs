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
    public class BusinessServiceAreaRepository : IBusinessServiceAreaRepository
    {
        private readonly HomeCycleDbContext _db;

        public BusinessServiceAreaRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(business_service_area serviceArea, CancellationToken cancellationToken = default)
        {
            // Map từ Domain Entity sạch sang Persistence Model để EF Core lưu trữ
            var entity = serviceArea.ToInfrastructure();
            await _db.Business_Service_Areas.AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<business_service_area> serviceAreas, CancellationToken cancellationToken = default)
        {
            var entities = serviceAreas.Select(sa => sa.ToInfrastructure()).ToList();
            await _db.Business_Service_Areas.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<List<business_service_area>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var entities = await _db.Business_Service_Areas
                .AsNoTracking()
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var serviceAreas = await _db.Business_Service_Areas
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            if (serviceAreas.Any())
            {
                _db.Business_Service_Areas.RemoveRange(serviceAreas);
            }
        }
    }
}
