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

        public Task<bool> HasOpenReportAsync(Guid postId, CancellationToken cancellationToken = default)
            => _db.Disputes.AsNoTracking().AnyAsync(x => x.PostId == postId
                && x.DisputeTargetType == (int)DisputeTargetType.Post
                && (x.DisputeStatus == (int)DisputeStatus.Pending || x.DisputeStatus == (int)DisputeStatus.UnderReview), cancellationToken);

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
            var local = _db.Posts.Local.FirstOrDefault(p => p.PostId == infraPost.PostId);
            if (local != null) _db.Entry(local).CurrentValues.SetValues(infraPost);
            else _db.Posts.Update(infraPost);
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
                .Include(x => x.Product)
                    .ThenInclude(x => x.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .FirstOrDefaultAsync(x => x.PostId == postId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<post?> GetByIdForUpdateAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            //FOR UPDATE khóa dòng Post để serialize việc trừ RemainingQuantity
            //giữa các Negotiation khác nhau trên cùng một bài đăng.
            //AsNoTracking: tránh xung đột ChangeTracker với UpdateAsync.
            var entity = await _db.Posts
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ""Post""
                    WHERE ""PostId"" = {postId}
                    FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<post?> GetDetailByIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Posts.AsNoTracking()
                .Include(x => x.User)
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

        // Trả về TẤT CẢ bài đăng bất kể trạng thái (Active/Suspended/Closed/Deleted).
        // Dành riêng cho Moderator/Admin quản lý hệ thống.
        public async Task<PagedResult<post>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.Product)
                .OrderByDescending(x => x.CreatedAt);

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

        // Chỉ trả về các bài đăng đang hoạt động (Active) — dành cho trang chủ phía người dùng.
        // Loại trừ các bài đã bị đình chỉ (Suspended), đóng (Closed) hoặc xóa (Deleted).
        public async Task<PagedResult<post>> GetAllActiveAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.Product)
                .Where(x => x.Status == (int)PostStatus.Active && (x.ExpiryDate == null || x.ExpiryDate > DateTime.UtcNow))
                .OrderByDescending(x => x.CreatedAt);

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

        public async Task<PagedResult<post>> GetAllByOwnerAsync(Guid ownerId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Include(x => x.Product)
                    .ThenInclude(x => x.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Brand)
                .Where(x => x.OwnerId == ownerId && x.Status != (int)PostStatus.Deleted)
                .OrderByDescending(x => x.CreatedAt);

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

        public async Task<post?> GetDetailByOwnerAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Posts.AsNoTracking()
                .Include(x => x.User)
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
                    x => x.PostId == postId && x.OwnerId == ownerId && x.Status != (int)PostStatus.Deleted, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<post>> DiscoverBusinessAsync(
            Guid userId,
            HomeCycle.Application.DTOs.Responses.Profiles.BusinessSurveyDetailResponse survey,
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var cities = survey.TargetCities.Where(city => !string.IsNullOrWhiteSpace(city))
                .Select(city => city.Trim().ToLowerInvariant()).Distinct().ToArray();
            var productTypeIds = survey.ProductTypeIds.Distinct().ToArray();
            var damageLevels = survey.AcceptableDamageLevels.Distinct().ToArray();
            var functionalityStatuses = survey.AcceptableFunctionalityStatuses.Distinct().ToArray();
            var retail = survey.ProcurementScales.Contains((int)ProcurementScale.Retail);
            var bulkLot = survey.ProcurementScales.Contains((int)ProcurementScale.BulkLot);
            var now = DateTime.UtcNow;

            var query = _db.Posts.AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Product).ThenInclude(x => x!.Category)
                .Include(x => x.Product).ThenInclude(x => x!.ProductType)
                .Include(x => x.Product).ThenInclude(x => x!.Brand)
                .Where(x => x.PostType == (int)PostType.Sell &&
                    x.Status == (int)PostStatus.Active &&
                    (!x.ExpiryDate.HasValue || x.ExpiryDate > now) &&
                    x.RemainingQuantity > 0 && x.OwnerId != userId &&
                    x.User != null && x.User.Status == (int)UserStatus.Active &&
                    x.City != null && cities.Contains(x.City.Trim().ToLower()) &&
                    x.Product != null &&
                    x.Product.ProductTypeId.HasValue && productTypeIds.Contains(x.Product.ProductTypeId.Value) &&
                    x.Product.DamageLevel.HasValue && damageLevels.Contains(x.Product.DamageLevel.Value) &&
                    x.Product.FunctionalityStatus.HasValue && functionalityStatuses.Contains(x.Product.FunctionalityStatus.Value) &&
                    ((retail && x.RemainingQuantity == 1) || (bulkLot && x.RemainingQuantity >= 2)));

            var totalCount = await query.CountAsync(cancellationToken);
            var entities = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PostId)
                .Skip(checked((request.PageNumber - 1) * request.PageSize))
                .Take(request.PageSize).ToListAsync(cancellationToken);

            return new PagedResult<post>
            {
                Items = entities.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<post>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Product)
                    .ThenInclude(x => x!.Category)
                .Include(x => x.Product)
                    .ThenInclude(x => x!.ProductType)
                .Include(x => x.Product)
                    .ThenInclude(x => x!.Brand)
                .Where(x =>
                    x.Status == (int)PostStatus.Active && (x.ExpiryDate == null || x.ExpiryDate > DateTime.UtcNow) &&
                    x.Product != null &&
                    x.User!.Status == (int)UserStatus.Active);

            // ==================== KEYWORD ====================

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = $"%{request.Keyword.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.Description ?? string.Empty, keyword) ||
                    EF.Functions.ILike(x.Product!.ProductName ?? string.Empty, keyword) ||
                    (x.Product.Category != null && EF.Functions.ILike(x.Product.Category.CategoryName ?? string.Empty, keyword)) ||
                    (x.Product.ProductType != null && EF.Functions.ILike(x.Product.ProductType.ProductTypeName ?? string.Empty, keyword)) ||

                    (x.Product.Brand != null &&
                    EF.Functions.ILike(x.Product.Brand.BrandName ?? string.Empty, keyword)));
                    //EF.Functions.ILike(x.Product.Brand.BrandName ?? string.Empty, keyword)) ||
                    //EF.Functions.ILike(x.City ?? string.Empty, keyword) ||
                    //EF.Functions.ILike(x.Ward ?? string.Empty, keyword) ||
                    //EF.Functions.ILike(x.StreetAddress ?? string.Empty, keyword));
            }

            // ==================== POST TYPE + CATEGORY + PRODUCT TYPE + BRAND + CITY ====================

            if (request.PostType.HasValue)
            {
                var postType = (int)request.PostType.Value;
                query = query.Where(x =>  x.PostType == postType);
            }

            if (request.CategoryId.HasValue)
            {
                var categoryId = request.CategoryId.Value;
                query = query.Where(x => x.Product!.CategoryId == categoryId);
            }

            if (request.ProductTypeId.HasValue)
            {
                var productTypeId = request.ProductTypeId.Value;
                query = query.Where(x => x.Product!.ProductTypeId == productTypeId);
            }

            if (request.BrandId.HasValue)
            {
                var brandId = request.BrandId.Value;
                query = query.Where(x => x.Product!.BrandId == brandId);
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();

                // So khớp chính xác nhưng không phân biệt hoa/thường.
                // Không thêm % nếu không muốn "Hồ Chí" khớp "Hồ Chí Minh".
                query = query.Where(x => EF.Functions.ILike(x.City ?? string.Empty, city));
            }

            // ==================== PRODUCT CONDITION ====================

            if (request.FunctionalityStatus.HasValue)
            {
                var functionalityStatus = (int)request.FunctionalityStatus.Value;
                query = query.Where(x => x.Product!.FunctionalityStatus == functionalityStatus);
            }

            if (request.SpaceUsage.HasValue)
            {
                var spaceUsage = (int)request.SpaceUsage.Value;
                query = query.Where(x => x.Product!.SpaceUsage == spaceUsage);
            }

            if (request.MinUsageDuration.HasValue)
            {
                var minUsageDuration = request.MinUsageDuration.Value;
                query = query.Where(x => x.Product!.UsageDuration >= minUsageDuration);
            }

            if (request.MaxUsageDuration.HasValue)
            {
                var maxUsageDuration = request.MaxUsageDuration.Value;
                query = query.Where(x => x.Product!.UsageDuration <= maxUsageDuration);
            }

            if (request.MinDamageLevel.HasValue)
            {
                var minDamageLevel = request.MinDamageLevel.Value;
                query = query.Where(x => x.Product!.DamageLevel >= minDamageLevel);
            }

            if (request.MaxDamageLevel.HasValue)
            {
                var maxDamageLevel = request.MaxDamageLevel.Value;
                query = query.Where(x => x.Product!.DamageLevel <= maxDamageLevel);
            }

            // ==================== PRICE ====================

            if (request.MinPrice.HasValue)
            {
                var minPrice = request.MinPrice.Value;
                query = query.Where(x => x.PostType == (int)PostType.Buy ? x.BasePrice == null || x.BasePrice >= minPrice : x.BasePrice >= minPrice);
            }

            if (request.MaxPrice.HasValue)
            {
                var maxPrice = request.MaxPrice.Value;
                query = query.Where(x => x.PostType == (int)PostType.Buy ? x.MinExpectedPrice == null || x.MinExpectedPrice <= maxPrice : x.BasePrice <= maxPrice);
            }

            // ==================== AVAILABILITY ====================

            if (request.OnlyAvailable == true)
                query = query.Where(x => x.RemainingQuantity > 0);

            // ==================== POSTED TIME ====================

            if (request.PostedWithinDays.HasValue)
            {
                var threshold = DateTime.UtcNow.AddDays(
                    -request.PostedWithinDays.Value);

                query = query.Where(x => x.CreatedAt >= threshold);
            }

            // ==================== DELIVERY METHOD ====================

            if (request.DeliveryMethod.HasValue)
            {
                var deliveryMethod = (int)request.DeliveryMethod.Value;
                query = query.Where(x => x.DeliveryMethod == deliveryMethod);
            }

            // ==================== PRIORITY LEVEL ====================

            if (request.PriorityLevel.HasValue)
            {
                var priorityLevel = (int)request.PriorityLevel.Value;
                query = query.Where(x => x.PriorityLevel == priorityLevel);
            }

            // ==================== ATTRIBUTE + OPTION ====================

            if (request.AttributeFilters is { Count: > 0 })
            {
                foreach (var filter in request.AttributeFilters)
                {
                    var attributeId = filter.AttributeId;

                    var optionIds = filter.OptionIds?
                        .Where(x => x != Guid.Empty)
                        .Distinct()
                        .ToArray()
                        ?? Array.Empty<Guid>();

                    // Validator nên ngăn trường hợp này.
                    if (attributeId == Guid.Empty || optionIds.Length == 0)
                        continue;

                    query = query.Where(postEntity =>
                        postEntity.Product!
                            .Product_Attribute_Values
                            .Any(value =>
                                value.AttributeId == attributeId &&

                                // Chỉ cho phép lọc attribute được phép
                                value.Attribute.IsFilterable &&

                                // Attribute phải thuộc đúng ProductType của Product
                                value.Attribute.ProductTypeId ==
                                postEntity.Product.ProductTypeId &&

                                value.OptionId.HasValue &&
                                optionIds.Contains(value.OptionId.Value)));
                }
            }

            // Đếm sau toàn bộ filter nhưng trước ORDER BY.
            var totalCount = await query.CountAsync(cancellationToken);

            // ==================== SORT ====================

            var sortBy = request.SortBy ?? PostSortBy.Newest;

            // PriorityLevel luôn được ưu tiên trước.
            var orderedQuery = query.OrderByDescending(x => x.PriorityLevel);

            orderedQuery = sortBy switch
            {
                PostSortBy.PriceAsc => orderedQuery
                    .ThenBy(x => x.BasePrice == null)
                    .ThenBy(x => x.BasePrice)
                    .ThenByDescending(x => x.CreatedAt),

                PostSortBy.PriceDesc => orderedQuery
                    .ThenBy(x => x.BasePrice == null)
                    .ThenByDescending(x => x.BasePrice)
                    .ThenByDescending(x => x.CreatedAt),

                PostSortBy.Oldest => orderedQuery
                    .ThenBy(x => x.CreatedAt),

                _ => orderedQuery
                    .ThenByDescending(x => x.CreatedAt)
            };

            orderedQuery = orderedQuery.ThenBy(x => x.PostId);

            // ==================== PAGINATION ====================

            var skip = (request.PageNumber - 1) *  request.PageSize;

            var entities = await orderedQuery
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<post>
            {
                Items = entities
                    .Select(x => x.ToDomain())
                    .ToList(),

                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> CountActiveByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _db.Posts.CountAsync(
                x => x.OwnerId == ownerId && x.Status == (int)PostStatus.Active && (x.ExpiryDate == null || x.ExpiryDate > DateTime.UtcNow),
                cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var dbPost = await _db.Posts.FindAsync(new object[] { postId }, cancellationToken);
            if (dbPost == null) return false;

            _db.Posts.Remove(dbPost);
            return true;
        }

    public async Task<int> GetAgreedBuyQuantityAsync(Guid postId, Guid? excludedNegotiationId = null, CancellationToken cancellationToken = default) =>
        await _db.Agreement_Forms.AsNoTracking()
            .Where(a => a.Negotiation.Offer.BuyPostId == postId &&
                (!excludedNegotiationId.HasValue || a.NegotiationId != excludedNegotiationId.Value) &&
                a.AgreementStatus != (int)AgreementStatus.Cancelled && a.AgreementStatus != (int)AgreementStatus.Expired &&
                (a.Order == null || a.Order.OrderStatus != (int)OrderStatus.Cancelled))
            .SumAsync(a => (int?)a.Quantity, cancellationToken) ?? 0;

    private IQueryable<Negotiation> Reservations(Guid postId, Guid? excluded = null) => _db.Negotiations.AsNoTracking()
        .Where(n => (n.PostId == postId || n.Offer.BuyPostId == postId) && n.NegotiationId != excluded &&
            (n.Agreement_Form != null
                ? (n.Agreement_Form.AgreementStatus == (int)AgreementStatus.Pending || n.Agreement_Form.AgreementStatus == (int)AgreementStatus.Awaiting_Payment)
                : (n.NegotiationStatus == null || n.NegotiationStatus == (int)NegotiationStatus.Open ||
                   n.NegotiationStatus == (int)NegotiationStatus.Agreed || n.NegotiationStatus == (int)NegotiationStatus.AgreementPending)));

    public async Task<int> GetReservedQuantityAsync(Guid postId, Guid? excludedNegotiationId = null, CancellationToken cancellationToken = default) =>
        await Reservations(postId, excludedNegotiationId).SumAsync(n => (int?)(n.Agreement_Form != null ? n.Agreement_Form.Quantity :
            n.NegotiationStatus == (int)NegotiationStatus.Open ? n.Offer.OfferQuantity : n.FinalQuantity ?? n.Offer.OfferQuantity), cancellationToken) ?? 0;

    public async Task<bool> HasUnfinishedTransactionsAsync(Guid postId, CancellationToken cancellationToken = default) =>
        await Reservations(postId).AnyAsync(cancellationToken) ||
        await _db.Orders.AnyAsync(o => (o.PostId == postId || o.Agreement.Negotiation.Offer.BuyPostId == postId) &&
            (o.OrderStatus == (int)OrderStatus.Pending || o.OrderStatus == (int)OrderStatus.Processing || o.OrderStatus == (int)OrderStatus.Disputing), cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetExpiredBuyPostIdsAsync(DateTime now, int count, CancellationToken cancellationToken = default) =>
        await _db.Posts.Where(p => p.PostType == (int)PostType.Buy && p.Status == (int)PostStatus.Active && p.ExpiryDate <= now)
            .OrderBy(p => p.ExpiryDate).Select(p => p.PostId).Take(count).ToListAsync(cancellationToken);
    public async Task<offer?> GetTradeByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Agreement_Forms.AsNoTracking().Where(a => a.AgreementId == agreementId).Select(a => a.Negotiation.Offer).FirstOrDefaultAsync(cancellationToken);
        return row?.ToDomain();
    }
    public async Task<offer?> GetTradeByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Orders.AsNoTracking().Where(o => o.OrderId == orderId).Select(o => o.Agreement.Negotiation.Offer).FirstOrDefaultAsync(cancellationToken);
        return row?.ToDomain();
    }
    public async Task RestoreOrderQuantityAsync(Guid orderId, bool restoreSellStock, CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction == null) throw new InvalidOperationException("Quantity restoration requires a transaction.");
        var order = await _db.Orders.AsNoTracking().Include(o => o.Agreement).ThenInclude(a => a.Negotiation).ThenInclude(n => n.Offer)
            .SingleAsync(o => o.OrderId == orderId, cancellationToken);
        // Caller holds the order lock and changes its terminal state in the same transaction.
        if (order.OrderStatus == (int)OrderStatus.Cancelled || order.OrderStatus == (int)OrderStatus.Returned) return;
        var offer = order.Agreement.Negotiation.Offer;
        var ids = new Guid?[] { restoreSellStock ? offer.PostId : null, offer.BuyPostId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x);
        foreach (var id in ids)
        {
            var post = await GetByIdForUpdateAsync(id, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy bài đăng cần hoàn số lượng.");
            var restoredRemainingQuantity = checked(post.RemainingQuantity + order.Quantity);
            if (post.PostType == PostType.Sell)
            {
                // A sell post may have been replenished since this order consumed stock.
                post.Quantity = Math.Max(post.Quantity, restoredRemainingQuantity);
            }
            else if (restoredRemainingQuantity > post.Quantity)
            {
                throw new InvalidOperationException("Số lượng hoàn vượt tổng số lượng bài đăng.");
            }
            post.RemainingQuantity = restoredRemainingQuantity;
            post.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(post, cancellationToken);
        }
    }
    }
}
