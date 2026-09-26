using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Commons.Helpers;

public static class TradingPostRules
{
    public static readonly TimeSpan ResponseTimeout = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PaymentTimeout = TimeSpan.FromHours(24);
    // Bản ghi tạo trước mốc này là dữ liệu cũ, không áp dụng timeout. Chỉnh lại theo thời điểm deploy.
    public static readonly DateTime TimeoutEffectiveFromUtc = new(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc);

    // Deadline tính từ CreatedAt (không lưu DB); CreatedAt chỉ gán khi tạo nên sửa/xác nhận không gia hạn.
    public static DateTime? ResponseDeadline(DateTime createdAt) =>
        createdAt >= TimeoutEffectiveFromUtc ? createdAt + ResponseTimeout : null;
    public static DateTime? PaymentDeadline(DateTime createdAt) =>
        createdAt >= TimeoutEffectiveFromUtc ? createdAt + PaymentTimeout : null;

    public static DateTime? ResponseDeadline(offer? o) => o == null ? null : ResponseDeadline(o.CreatedAt);
    public static DateTime? ResponseDeadline(message? m) =>
        m?.MessageType is MessageType.Offer or MessageType.CounterOffer ? ResponseDeadline(m.CreatedAt) : null;
    public static DateTime? PaymentDeadline(agreement_form? a) => a == null ? null : PaymentDeadline(a.CreatedAt);

    // now >= deadline là hết hạn; so sánh bằng UTC.
    public static bool IsExpired(DateTime? deadline) => deadline.HasValue && DateTime.UtcNow >= deadline.Value;

    // Dùng cho truy vấn DB: đến hạn khi TimeoutEffectiveFromUtc <= CreatedAt <= giá trị trả về.
    public static DateTime ResponseDueCreatedBefore(DateTime now) => now - ResponseTimeout;
    public static DateTime PaymentDueCreatedBefore(DateTime now) => now - PaymentTimeout;

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
            if (p.PostType == PostType.Buy)
            {
                var targetAvailable = p.Quantity - await repo.GetAgreedBuyQuantityAsync(id, excludedNegotiationId, ct);
                if (quantity > targetAvailable)
                    return OfferErrors.QuantityExceedsRemaining(quantity, Math.Max(0, targetAvailable));
            }
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
