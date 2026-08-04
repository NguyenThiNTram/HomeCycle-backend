using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Externals;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Products
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly HomeCycleDbContext _db;

        public CategoryRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(category category, CancellationToken cancellationToken = default)
        {
            var entity = category.ToInfrastructure();

            await _db.Categories.AddAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(category category, CancellationToken cancellationToken = default)
        {
            var entity = category.ToInfrastructure();

            var localEntry = _db.Categories.Local
                .FirstOrDefault(x => x.CategoryId == entity.CategoryId);

            if (localEntry != null)
            {
                _db.Entry(localEntry).State = EntityState.Detached;
            }

            _db.Categories.Update(entity);
            await Task.CompletedTask; //test thử, nếu không lỗi thì để yên
        }

        public async Task<category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<category?> GetByNameAsync(string categoryName, CancellationToken cancellationToken = default)
        {
            string trimmedName = (categoryName ?? string.Empty).Trim();

            //không phân biệt chữ hoa/thường bằng ILike nguyên bản của Postgres -- test thử, nếu không lỗi thì để yên
            var entity = await _db.Categories
                .FirstOrDefaultAsync(x => EF.Functions.ILike(x.CategoryName, trimmedName), cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByNameAsync(string categoryName, CancellationToken cancellationToken = default)
        {
            return await _db.Categories.AnyAsync(
                x => x.CategoryName!.ToLower() == categoryName.Trim().ToLower(),
                cancellationToken);
        }

        public async Task<PagedResult<category>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            return await _db.Categories
                .AsNoTracking()
                .OrderBy(x => x.CategoryName)
                .Select(x => x.ToDomain())
                .ToPagedResultAsync(
                    pagination.PageNumber,
                    pagination.PageSize,
                    cancellationToken);
        }

        public async Task<PagedResult<category>> GetActiveAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            return await _db.Categories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.CategoryName)
                .Select(x => x.ToDomain())
                .ToPagedResultAsync(
                    pagination.PageNumber,
                    pagination.PageSize,
                    cancellationToken);
        }

        public async Task<PagedResult<category>> SearchAsync(string keyword, PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var query = _db.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.CategoryName!, $"%{keyword}%") ||
                    EF.Functions.ILike(x.Description!, $"%{keyword}%"));
            }

            return await query
                .OrderBy(x => x.CategoryName)
                .Select(x => x.ToDomain())
                .ToPagedResultAsync(
                    pagination.PageNumber,
                    pagination.PageSize,
                    cancellationToken);
        }
    }
}
