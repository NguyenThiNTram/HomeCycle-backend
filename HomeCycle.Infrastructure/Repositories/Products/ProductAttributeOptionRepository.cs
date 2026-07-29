using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using MathNet.Numerics.Statistics.Mcmc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Products
{
    public class ProductAttributeOptionRepository : IProductAttributeOptionRepository
    {
        private readonly HomeCycleDbContext _db;

        public ProductAttributeOptionRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(product_attribute_option entity, CancellationToken cancellationToken = default)
        {
            var infraOpt = entity.ToInfrastructure();
            await _db.Product_Attribute_Options.AddAsync(infraOpt, cancellationToken);
        }

        public async Task UpdateAsync(product_attribute_option entity, CancellationToken cancellationToken = default)
        {
            var infra = entity.ToInfrastructure();
            var tracked = await _db.Product_Attribute_Options.FindAsync(new object[] { infra.OptionId }, cancellationToken);
            if (tracked != null)
                _db.Entry(tracked).CurrentValues.SetValues(infra);
            else
                _db.Product_Attribute_Options.Update(infra);
        }

        public async Task<product_attribute_option?> GetByIdAsync(
            Guid optionId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Attribute_Options
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.OptionId == optionId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<IReadOnlyList<product_attribute_option>> GetByAttributeAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Attribute_Options
                .AsNoTracking()
                .Where(x => x.AttributeId == attributeId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ToDomain())
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid attributeId, string optionValue, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Attribute_Options.AnyAsync(
                x =>
                    x.AttributeId == attributeId &&
                    EF.Functions.ILike(
                        x.OptionValue!,
                        optionValue.Trim()),
                cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid optionId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Attribute_Options
                .FirstOrDefaultAsync(x => x.OptionId == optionId, cancellationToken);

            if (entity is null)
                return false;

            _db.Product_Attribute_Options.Remove(entity);
            return true;
        }
    }
}
