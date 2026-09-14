using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Commons.Helpers;

public static class TradingPostRules
{
    public static bool IsAvailable(post p) => p.Status == PostStatus.Active &&
        (!p.ExpiryDate.HasValue || p.ExpiryDate > DateTime.UtcNow) && p.RemainingQuantity > 0;

    // Call before locking Offer/Negotiation/Agreement rows, in an active transaction.
    public static async Task LockAsync(this IPostRepository repo, Guid sellId, Guid? buyId, CancellationToken ct)
    {
        foreach (var id in new Guid?[] { sellId, buyId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x))
            await repo.GetByIdForUpdateAsync(id, ct);
    }

    public static async Task<Error?> ValidateCapacityAsync(this IPostRepository repo, offer offer, int quantity,
        Guid? excludedNegotiationId, bool requireActive, CancellationToken ct)
    {
        if (quantity <= 0) return OfferErrors.InvalidQuantity;
        foreach (var id in new Guid?[] { offer.PostId, offer.BuyPostId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct())
        {
            var p = await repo.GetByIdAsync(id, ct);
            if (p is null) return OfferErrors.PostNotFound;
            if (requireActive && !IsAvailable(p)) return OfferErrors.PostNotActive;
            if (p.Status is PostStatus.Deleted or PostStatus.Suspended) return OfferErrors.PostNotActive;
            var available = p.RemainingQuantity - await repo.GetReservedQuantityAsync(id, excludedNegotiationId, ct);
            if (quantity > available) return OfferErrors.QuantityExceedsRemaining(quantity, Math.Max(0, available));
        }
        return null;
    }

    public static (Guid SellerId, Guid BuyerId) Participants(post sellPost, offer offer)
    {
        // Historical offers on a Buy Post retain their original participant direction.
        if (sellPost.PostType == PostType.Buy) return (offer.SenderId, offer.ReceiverId);
        if (sellPost.OwnerId != offer.SenderId && sellPost.OwnerId != offer.ReceiverId)
            throw new InvalidOperationException("Chủ bài bán không thuộc các bên của đề nghị.");
        return (sellPost.OwnerId, offer.SenderId == sellPost.OwnerId ? offer.ReceiverId : offer.SenderId);
    }
}
