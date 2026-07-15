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
    public class BusinessProductTypeRepository : IBusinessProductTypeRepository
    {
        private readonly HomeCycleDbContext _db;

        public BusinessProductTypeRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(business_product_type productType, CancellationToken cancellationToken = default)
        {
            var entity = productType.ToInfrastructure();

            await _db.Business_Product_Types.AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<business_product_type> productTypes, CancellationToken cancellationToken = default)
        {
            var entities = productTypes.Select(pt => pt.ToInfrastructure()).ToList();
            await _db.Business_Product_Types.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<List<business_product_type>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var entities = await _db.Business_Product_Types
                .AsNoTracking()
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var productTypes = await _db.Business_Product_Types
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            if (productTypes.Any())
            {
                _db.Business_Product_Types.RemoveRange(productTypes);
            }
        }
    }
}
