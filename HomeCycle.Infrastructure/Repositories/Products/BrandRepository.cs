using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Brands;
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
    public class BrandRepository : IBrandRepository
    {
        private readonly HomeCycleDbContext _db;

        public BrandRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(brand brand, CancellationToken cancellationToken = default)
        {
            var entity = brand.ToInfrastructure();

            await _db.Brands.AddAsync(
                entity,
                cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string brandName, CancellationToken cancellationToken = default)
        {
            return await _db.Brands
                .AnyAsync(
                    x => EF.Functions.ILike(
                        x.BrandName,
                        brandName.Trim()),
                    cancellationToken);
        }

        public async Task<PagedResult<brand>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Brands
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(
                cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<brand>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Brands
               .AsNoTracking()
               .FirstOrDefaultAsync(
                   x => x.BrandId == brandId,
                   cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<brand>> SearchAsync(string keyword, BrandSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Brands.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.BrandName,
                        $"%{keyword}%"));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(
                cancellationToken);

            var items = await query
                .OrderBy(x => x.BrandName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<brand>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task UpdateAsync(brand brand, CancellationToken cancellationToken = default)
        {
            var entity = brand.ToInfrastructure();
            _db.Brands.Update(entity);
        }

        public async Task<PagedResult<brand>> GetActiveAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var query = await _db.Brands
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.BrandName)
                .Select(x => x.ToDomain())
                .ToListAsync(cancellationToken);

            return new PagedResult<brand>
            {
                Items = query,
                PageNumber = 1,
                PageSize = query.Count,
                TotalCount = query.Count,
                TotalPages = 1
            };
        }

    }
}
