using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Products;
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
    public class ProductTypeRepository : IProductTypeRepository
    {
        private readonly HomeCycleDbContext _db;

        public ProductTypeRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(product_type entity, CancellationToken cancellationToken = default)
        {
            var type = entity.ToInfrastructure();

            await _db.Product_Types.AddAsync(
                type,
                cancellationToken);
        }

        public Task UpdateAsync(product_type entity, CancellationToken cancellationToken = default)
        {
            _db.Product_Types.Update(entity.ToInfrastructure());
            return Task.CompletedTask;
        }

        public async Task<product_type?> AggregateUpdateAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Types
                .Include(x => x.Product_Attributes)
                    .ThenInclude(x => x.Product_Attribute_Options)
                .FirstOrDefaultAsync(
                    x => x.ProductTypeId == productTypeId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByNameAsync(Guid categoryId, string productTypeName, CancellationToken cancellationToken = default)
        {
            return await _db.Product_Types.AnyAsync(x =>
                x.CategoryId == categoryId &&
                EF.Functions.ILike(
                    x.ProductTypeName!,
                    productTypeName.Trim()),
            cancellationToken);
        }

        public async Task<PagedResult<product_type>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Product_Types
            .AsNoTracking()
            .OrderBy(x => x.ProductTypeName);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<product_type>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<product_type>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            var dbTypes = await _db.Product_Types
                .Where(x => x.CategoryId == categoryId && x.IsActive)
                .ToListAsync(cancellationToken);

            return dbTypes.Select(x => x.ToDomain());
        }

        public async Task<product_type?> GetByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Product_Types
            .AsNoTracking()
            .Include(pt => pt.Product_Attributes).ThenInclude(pa => pa.Product_Attribute_Options)
            .FirstOrDefaultAsync(
                x => x.ProductTypeId == productTypeId,
                cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<product_type>> SearchAsync(ProductTypeSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Product_Types
            .AsNoTracking()
            .AsQueryable();

            if (request.CategoryId.HasValue)
                query = query.Where(x => x.CategoryId == request.CategoryId);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(x => EF.Functions.ILike(x.ProductTypeName!, $"%{request.Keyword.Trim()}%"));

            if (request.IsActive.HasValue)
                query = query.Where(x => x.IsActive == request.IsActive);

            query = query.OrderBy(x => x.ProductTypeName);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<product_type>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> DeleteAsync(Guid productTypeId, CancellationToken cancellationToken)
        {
            var dbType = await _db.Product_Types.FindAsync(new object[] { productTypeId }, cancellationToken);
            if (dbType == null) return false;

            dbType.IsActive = false;
            return true;
        }
    }
}
