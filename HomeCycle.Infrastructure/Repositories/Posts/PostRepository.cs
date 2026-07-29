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
            var query = _db.Posts
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(p => p!.Brand)
                .Where(x => x.Status == (int)PostStatus.Active)
                .AsQueryable();

            // ---------- KEYWORD ----------
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = $"%{request.Keyword.Trim()}%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.Description ?? "", kw) ||
                    EF.Functions.ILike(x.Product!.ProductName ?? "", kw) ||
                    (x.Product!.Brand != null && EF.Functions.ILike(x.Product.Brand.BrandName, kw)) ||
                    EF.Functions.ILike(x.City ?? "", kw) ||
                    EF.Functions.ILike(x.Ward ?? "", kw) ||
                    EF.Functions.ILike(x.StreetAddress ?? "", kw));
            }

            // ---------- PHÂN LOẠI BÁN/MUA ----------
            if (request.PostType.HasValue)
                query = query.Where(x => x.PostType == (int)request.PostType);

            // ---------- FILTER PHÂN CẤP ----------
            if (request.CategoryId.HasValue)
                query = query.Where(x => x.Product!.CategoryId == request.CategoryId);

            if (request.ProductTypeId.HasValue)
                query = query.Where(x => x.Product!.ProductTypeId == request.ProductTypeId);

            if (request.BrandId.HasValue)
                query = query.Where(x => x.Product!.BrandId == request.BrandId);

            // ---------- FILTER ĐIỀU KIỆN SẢN PHẨM ----------

            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(x => x.City == request.City);

            if (request.FunctionalityStatus.HasValue)
                query = query.Where(x => x.Product!.FunctionalityStatus == (int)request.FunctionalityStatus);

            if (request.MinUsageDuration.HasValue)
                query = query.Where(x => x.Product!.UsageDuration >= request.MinUsageDuration);
            if (request.MaxUsageDuration.HasValue)
                query = query.Where(x => x.Product!.UsageDuration <= request.MaxUsageDuration);

            if (request.MinDamageLevel.HasValue)
                query = query.Where(x => x.Product!.DamageLevel >= request.MinDamageLevel);
            if (request.MaxDamageLevel.HasValue)
                query = query.Where(x => x.Product!.DamageLevel <= request.MaxDamageLevel);

            // ---------- KHOẢNG GIÁ ----------
            if (request.MinPrice.HasValue)
                query = query.Where(x => x.BasePrice >= request.MinPrice);
            if (request.MaxPrice.HasValue)
                query = query.Where(x => x.BasePrice <= request.MaxPrice);

            // ---------- BASE FILTER NÂNG CAO TRẢI NGHIỆM ----------
            if (request.OnlyAvailable)
                query = query.Where(x => x.RemainingQuantity > 0);

            if (request.PostedWithinDays.HasValue)
            {
                var threshold = DateTime.UtcNow.AddDays(-request.PostedWithinDays.Value);
                query = query.Where(x => x.CreatedAt >= threshold);
            }

            if (request.DeliveryMethod.HasValue)
                query = query.Where(x => x.DeliveryMethod == (int)request.DeliveryMethod);

            if (request.PriorityLevel.HasValue)
                query = query.Where(x => x.PriorityLevel == (int)request.PriorityLevel);

            // ---------- FILTER ĐỘNG ----------
            // Product_Attribute_Value khác nhau — Join thường sẽ nhân bản dòng và lọc sai.
            if (request.ProductTypeId.HasValue && request.AttributeFilters is { Count: > 0 })
            {
                foreach (var filter in request.AttributeFilters)
                {
                    var attrId = filter.AttributeId;

                    if (filter.OptionIds is { Count: > 0 })
                    {
                        var optionIds = filter.OptionIds;
                        query = query.Where(x => x.Product!.Product_Attribute_Values
                            .Any(av => av.AttributeId == attrId && av.OptionId.HasValue && optionIds.Contains(av.OptionId.Value)));
                    }
                    else if (filter.MinValue.HasValue || filter.MaxValue.HasValue)
                    {
                        var min = filter.MinValue;
                        var max = filter.MaxValue;
                        query = query.Where(x => x.Product!.Product_Attribute_Values.Any(av =>
                            av.AttributeId == attrId &&
                            (!min.HasValue || av.ValueNumber >= min) &&
                            (!max.HasValue || av.ValueNumber <= max)));
                    }
                    else if (filter.ValueBoolean.HasValue)
                    {
                        var val = filter.ValueBoolean.Value;
                        query = query.Where(x => x.Product!.Product_Attribute_Values
                            .Any(av => av.AttributeId == attrId && av.ValueBoolean == val));
                    }
                    else if (!string.IsNullOrWhiteSpace(filter.ValueTextContains))
                    {
                        var text = $"%{filter.ValueTextContains.Trim()}%";
                        query = query.Where(x => x.Product!.Product_Attribute_Values
                            .Any(av => av.AttributeId == attrId && EF.Functions.ILike(av.ValueText ?? "", text)));
                    }
                }

            }

            // ---------- SORT ----------

            IOrderedQueryable<post> orderedQuery = (IOrderedQueryable<post>)query.OrderByDescending(x => x.PriorityLevel);

            query = request.SortBy switch
            {
                PostSortBy.PriceAsc => query.OrderBy(x => x.BasePrice == null).ThenBy(x => x.BasePrice),

                PostSortBy.PriceDesc => query.OrderBy(x => x.BasePrice == null).ThenByDescending(x => x.BasePrice),

                PostSortBy.Oldest => query.OrderBy(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };

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

        public async Task<int> CountActiveByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _db.Posts.CountAsync(
                x => x.OwnerId == ownerId && x.Status == (int)PostStatus.Active,
                cancellationToken);
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
