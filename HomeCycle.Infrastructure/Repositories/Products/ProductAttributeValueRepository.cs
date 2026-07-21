using Google;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Products
{
    public class ProductAttributeValueRepository : IProductAttributeValueRepository
    {
        private readonly HomeCycleDbContext _db;

        public ProductAttributeValueRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddRangeAsync(IEnumerable<product_attribute_value> entities, CancellationToken cancellationToken = default)
        {
            var infraEntities = entities.Select(e => e.ToInfrastructure());
            await _db.Product_Attribute_Values.AddRangeAsync(infraEntities, cancellationToken);
        }

        public async Task<IReadOnlyList<product_attribute_value>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Attribute_Values
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .Select(x => x.ToDomain())
                .ToListAsync(cancellationToken);
        }

        public async Task RemoveByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Product_Attribute_Values
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                _db.Product_Attribute_Values.RemoveRange(existing);
            }
        }
    }
}