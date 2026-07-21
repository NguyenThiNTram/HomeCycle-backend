using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Posts
{
    public class PostRepository : IPostRepository
    {
        private readonly HomeCycleDbContext _db;

        public PostRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(post entity, CancellationToken cancellationToken = default)
        {
            var infraPost = entity.ToInfrastructure();
            await _db.Posts.AddAsync(infraPost, cancellationToken);
        }

        public Task UpdateAsync(post entity, CancellationToken cancellationToken = default)
        {
            var infraPost = entity.ToInfrastructure();
            _db.Posts.Update(infraPost);
            return Task.CompletedTask;
        }

        public async Task<bool> UpdateStatusAsync(Guid postId, PostStatus status, CancellationToken cancellationToken = default)
{
    var dbPost = await _db.Posts.FindAsync(new object[] { postId }, cancellationToken);
    if (dbPost == null) return false;

    dbPost.Status = (int)status;
    dbPost.UpdatedAt = DateTime.UtcNow;
    return true;
}

        public async Task<post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PostId == postId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<post?> GetDetailByIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Posts.AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Product_Attribute_Values)
                        .ThenInclude(x => x.Attribute)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Product_Attribute_Values)
                        .ThenInclude(x => x.Option)
                .FirstOrDefaultAsync(
                    x => x.PostId == postId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<post>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts.AsNoTracking().OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<post>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<post>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.Description!, $"%{keyword}%") ||
                    EF.Functions.ILike(x.Product.ProductName!, $"%{keyword}%"));
            }

            if (request.CategoryId.HasValue)
                query = query.Where(x => x.Product!.CategoryId == request.CategoryId);

            if (request.BrandId.HasValue)
                query = query.Where(x => x.Product.BrandId == request.BrandId);

            if (request.ProductTypeId.HasValue)
                query = query.Where(x => x.Product!.ProductTypeId == request.ProductTypeId);

            if (request.BrandId.HasValue)
                query = query.Where(x => x.Product!.BrandId == request.BrandId);

            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(x => x.City == request.City);

            if (request.PostType.HasValue)
                query = query.Where(x => x.PostType == (int)request.PostType);

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == (int)request.Status);

            if (request.MinPrice.HasValue)
                query = query.Where(x => x.BasePrice >= request.MinPrice);

            if (request.MaxPrice.HasValue)
                query = query.Where(x => x.BasePrice <= request.MaxPrice);

            if (request.IsBusinessPosting.HasValue)
                query = query.Where(x => x.IsBusinessPosting == request.IsBusinessPosting);

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<post>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> DeleteAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var dbPost = await _db.Posts.FindAsync(new object[] { postId }, cancellationToken);
            if (dbPost == null) return false;

            dbPost.Status = (int)PostStatus.Deleted;
            dbPost.UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }
}
