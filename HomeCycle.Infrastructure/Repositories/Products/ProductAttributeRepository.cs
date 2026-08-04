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
    public class ProductAttributeRepository : IProductAttributeRepository
    {
        private readonly HomeCycleDbContext _db;

        public ProductAttributeRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(product_attribute entity, CancellationToken cancellationToken = default)
        {
            var infraAttr = entity.ToInfrastructure();
            await _db.Product_Attributes.AddAsync(infraAttr, cancellationToken);
        }

        public async Task UpdateAsync(product_attribute entity, CancellationToken cancellationToken = default)
        {
            var infra = entity.ToInfrastructure();
            var tracked = await _db.Product_Attributes.FindAsync(new object[] { infra.AttributeId }, cancellationToken);
            if (tracked != null)
                _db.Entry(tracked).CurrentValues.SetValues(infra);
            else
                _db.Product_Attributes.Update(infra);
        }

        public async Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Attributes.FindAsync(new object[] { attributeId }, cancellationToken);
            if (entity is null) return false;

            _db.Product_Attributes.Remove(entity); // Cascade sẽ tự xóa Option con nếu DB config ON DELETE CASCADE, nếu không, cần xóa Option trước — xem lưu ý bên dưới.
            return true;
        }

        public async Task<bool> ExistsByNameAsync(Guid productTypeId, string attributeName, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Attributes.AnyAsync(
                x =>
                    x.ProductTypeId == productTypeId &&
                    EF.Functions.ILike(
                        x.AttributeName!,
                        attributeName.Trim()),
                cancellationToken);
        }

        public async Task<product_attribute?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Attributes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.AttributeId == attributeId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<IReadOnlyList<product_attribute>> GetByProductTypeAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Attributes
                .AsNoTracking()
                .Where(x => x.ProductTypeId == productTypeId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ToDomain())
                .ToListAsync(cancellationToken);
        }
    }
}
