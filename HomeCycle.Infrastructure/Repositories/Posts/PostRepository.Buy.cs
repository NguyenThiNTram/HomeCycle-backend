using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure.Repositories.Posts;

public partial class PostRepository
{
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
    private sealed class MatchCandidate
    {
        public Post Post { get; set; } = null!;
        public int Score { get; set; }
    }

    private IQueryable<MatchCandidate> BuildMatchQuery(post buyPost)
    {
        var b = buyPost.Product!;
        var now = DateTime.UtcNow;
        var query = _db.Posts.AsNoTracking().Where(p => p.PostType == (int)PostType.Sell && p.Product != null &&
            p.Status == (int)PostStatus.Active && p.User!.Status == (int)UserStatus.Active && p.OwnerId != buyPost.OwnerId &&
            (p.ExpiryDate == null || p.ExpiryDate > now) && p.RemainingQuantity > 0);
        if (b.CategoryId.HasValue) query = query.Where(p => p.Product!.CategoryId == b.CategoryId);
        if (b.ProductTypeId.HasValue) query = query.Where(p => p.Product!.ProductTypeId == b.ProductTypeId);
        // Only show stock that is not held by a negotiation or an unpaid agreement.
        query = query.Where(p => p.RemainingQuantity > (_db.Negotiations.Where(n => n.PostId == p.PostId &&
            (n.Agreement_Form != null ? (n.Agreement_Form.AgreementStatus == (int)AgreementStatus.Pending || n.Agreement_Form.AgreementStatus == (int)AgreementStatus.Awaiting_Payment) :
             (n.NegotiationStatus == null || n.NegotiationStatus == (int)NegotiationStatus.Open || n.NegotiationStatus == (int)NegotiationStatus.Agreed || n.NegotiationStatus == (int)NegotiationStatus.AgreementPending)))
            .Sum(n => (int?)(n.Agreement_Form != null ? n.Agreement_Form.Quantity : n.NegotiationStatus == (int)NegotiationStatus.Open ? n.Offer.OfferQuantity : n.FinalQuantity ?? n.Offer.OfferQuantity)) ?? 0));
        var city = string.IsNullOrWhiteSpace(buyPost.City) ? null : buyPost.City.Trim().ToLowerInvariant();
        var functionality = (int?)b.FunctionalityStatus;
        var damage = (int?)b.DamageLevel;
        var scores = query.Select(p => new MatchCandidate { Post = p, Score =
            (b.BrandId != null && p.Product!.BrandId == b.BrandId ? 1 : 0) +
            (functionality != null && p.Product!.FunctionalityStatus <= functionality ? 1 : 0) +
            (b.UsageDuration != null && p.Product!.UsageDuration <= b.UsageDuration ? 1 : 0) +
            (damage != null && p.Product!.DamageLevel <= damage ? 1 : 0) +
            (city != null && p.City != null && p.City.Trim().ToLower() == city ? 1 : 0) +
            ((buyPost.MinExpectedPrice != null || buyPost.BasePrice != null) && p.BasePrice != null &&
                (buyPost.MinExpectedPrice == null || p.BasePrice >= buyPost.MinExpectedPrice) &&
                (buyPost.BasePrice == null || p.BasePrice <= buyPost.BasePrice) ? 1 : 0) });
        foreach (var value in b.Product_Attribute_Values)
        {
            var id = value.AttributeId; var option = value.OptionId; var text = value.ValueText; var number = value.ValueNumber; var boolean = value.ValueBoolean;
            scores = scores.Select(x => new MatchCandidate { Post = x.Post, Score = x.Score + (x.Post.Product!.Product_Attribute_Values.Any(v =>
                v.AttributeId == id && v.OptionId == option && v.ValueText == text && v.ValueNumber == number && v.ValueBoolean == boolean) ? 1 : 0) });
        }
        return scores;
    }

    private IQueryable<Guid> BuildMatchPageQuery(post buyPost, PaginationRequest request) =>
        BuildMatchQuery(buyPost).OrderByDescending(x => x.Score).ThenByDescending(x => x.Post.CreatedAt).ThenBy(x => x.Post.PostId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => x.Post.PostId);

    public async Task<PagedResult<post>> GetMatchesAsync(post buyPost, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var count = await BuildMatchQuery(buyPost).CountAsync(cancellationToken);
        var ids = await BuildMatchPageQuery(buyPost, request).ToListAsync(cancellationToken);
        var rows = await _db.Posts.AsNoTracking().Where(p => ids.Contains(p.PostId)).Include(p => p.User)
            .Include(p => p.Product).ThenInclude(p => p!.Category).Include(p => p.Product).ThenInclude(p => p!.ProductType)
            .Include(p => p.Product).ThenInclude(p => p!.Brand).Include(p => p.Product).ThenInclude(p => p!.Product_Attribute_Values)
            .ToListAsync(cancellationToken);
        var byId = rows.ToDictionary(p => p.PostId);
        return new PagedResult<post> { Items = ids.Select(id => byId[id].ToDomain()).ToList(), TotalCount = count, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }
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
            if (post.RemainingQuantity + order.Quantity > post.Quantity) throw new InvalidOperationException("Số lượng hoàn vượt tổng số lượng bài đăng.");
            post.RemainingQuantity += order.Quantity; post.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(post, cancellationToken);
        }
    }
}
