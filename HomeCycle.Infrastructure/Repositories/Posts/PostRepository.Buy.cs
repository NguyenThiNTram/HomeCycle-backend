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
