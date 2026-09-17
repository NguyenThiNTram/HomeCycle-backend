using HomeCycle.Application.Interfaces.Repositories.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Application.SupplierMatching.Normalization;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Infrastructure.Repositories.SupplierMatching;

public sealed class SupplierMatchCandidateRepository(
    HomeCycleDbContext db,
    TimeProvider clock,
    ILogger<SupplierMatchCandidateRepository> logger) : ISupplierMatchCandidateRepository
{
    public Task<IReadOnlyList<SupplierCandidate>> GetCandidatesAsync(
        SupplierDemandContext demand,
        int candidateLimit,
        CancellationToken cancellationToken = default) =>
        LoadCandidatesAsync(demand, candidateLimit, null, cancellationToken);

    public Task<IReadOnlyList<SupplierCandidate>> GetCandidatesByIdsAsync(
        SupplierDemandContext demand,
        IReadOnlyCollection<Guid> sellPostIds,
        CancellationToken cancellationToken = default)
    {
        var ids = sellPostIds.Distinct().ToArray();
        return ids.Length == 0
            ? Task.FromResult<IReadOnlyList<SupplierCandidate>>([])
            : LoadCandidatesAsync(demand, ids.Length, ids, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierCandidateLiveState>> GetLiveStatesAsync(
        IReadOnlyCollection<Guid> sellPostIds,
        CancellationToken cancellationToken = default)
    {
        var ids = sellPostIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.Posts.AsNoTracking()
            .Where(post => ids.Contains(post.PostId))
            .Select(post => new SupplierCandidateLiveState(
                post.PostId,
                post.PostType == (int)PostType.Sell &&
                post.Status == (int)PostStatus.Active &&
                post.User != null && post.User.Status == (int)UserStatus.Active &&
                (!post.ExpiryDate.HasValue || post.ExpiryDate > now),
                Math.Max(0, post.RemainingQuantity -
                    (db.Negotiations
                        .Where(negotiation => negotiation.PostId == post.PostId &&
                            (negotiation.Agreement_Form != null
                                ? negotiation.Agreement_Form.AgreementStatus == (int)AgreementStatus.Pending ||
                                  negotiation.Agreement_Form.AgreementStatus == (int)AgreementStatus.Awaiting_Payment
                                : negotiation.NegotiationStatus == null ||
                                  negotiation.NegotiationStatus == (int)NegotiationStatus.Open ||
                                  negotiation.NegotiationStatus == (int)NegotiationStatus.Agreed ||
                                  negotiation.NegotiationStatus == (int)NegotiationStatus.AgreementPending))
                        .Sum(negotiation => (int?)(negotiation.Agreement_Form != null
                            ? negotiation.Agreement_Form.Quantity
                            : negotiation.NegotiationStatus == (int)NegotiationStatus.Open
                                ? negotiation.Offer.OfferQuantity
                                : negotiation.FinalQuantity ?? negotiation.Offer.OfferQuantity)) ?? 0)),
                post.BasePrice))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SupplierCandidate>> LoadCandidatesAsync(
        SupplierDemandContext demand,
        int candidateLimit,
        Guid[]? allowedPostIds,
        CancellationToken cancellationToken)
    {
        candidateLimit = Math.Clamp(candidateLimit, 1, 500);
        var now = clock.GetUtcNow().UtcDateTime;

        var query = db.Posts.AsNoTracking()
            .Where(post =>
                post.PostType == (int)PostType.Sell &&
                post.Status == (int)PostStatus.Active &&
                post.Product != null &&
                post.User != null &&
                post.User.Status == (int)UserStatus.Active &&
                (!demand.RequesterId.HasValue || post.OwnerId != demand.RequesterId.Value) &&
                (!post.ExpiryDate.HasValue || post.ExpiryDate > now) &&
                post.BasePrice > 0 &&
                post.RemainingQuantity > 0);

        if (allowedPostIds is not null)
            query = query.Where(post => allowedPostIds.Contains(post.PostId));

        if (demand.ProductTypeId.HasValue)
            query = query.Where(post => post.Product!.ProductTypeId == demand.ProductTypeId);
        else if (demand.CategoryId.HasValue)
            query = query.Where(post => post.Product!.CategoryId == demand.CategoryId);

        var rows = await query
            .Select(post => new CandidateRow
            {
                SellPostId = post.PostId,
                SupplierId = post.OwnerId,
                ProductId = post.Product!.ProductId,
                CategoryId = post.Product.CategoryId,
                ProductTypeId = post.Product.ProductTypeId,
                BrandId = post.Product.BrandId,
                ProductName = post.Product.ProductName,
                CategoryName = post.Product.Category != null ? post.Product.Category.CategoryName : null,
                ProductTypeName = post.Product.ProductType != null ? post.Product.ProductType.ProductTypeName : null,
                BrandName = post.Product.Brand != null ? post.Product.Brand.BrandName : null,
                ModelNumber = post.Product.ModelNumber,
                FunctionalityStatus = post.Product.FunctionalityStatus,
                DamageLevel = post.Product.DamageLevel,
                UsageDuration = post.Product.UsageDuration,
                Price = post.BasePrice,
                RemainingQuantity = post.RemainingQuantity,
                ReservedQuantity = db.Negotiations
                    .Where(negotiation => negotiation.PostId == post.PostId &&
                        (negotiation.Agreement_Form != null
                            ? negotiation.Agreement_Form.AgreementStatus == (int)AgreementStatus.Pending ||
                              negotiation.Agreement_Form.AgreementStatus == (int)AgreementStatus.Awaiting_Payment
                            : negotiation.NegotiationStatus == null ||
                              negotiation.NegotiationStatus == (int)NegotiationStatus.Open ||
                              negotiation.NegotiationStatus == (int)NegotiationStatus.Agreed ||
                              negotiation.NegotiationStatus == (int)NegotiationStatus.AgreementPending))
                    .Sum(negotiation => (int?)(negotiation.Agreement_Form != null
                        ? negotiation.Agreement_Form.Quantity
                        : negotiation.NegotiationStatus == (int)NegotiationStatus.Open
                            ? negotiation.Offer.OfferQuantity
                            : negotiation.FinalQuantity ?? negotiation.Offer.OfferQuantity)) ?? 0,
                City = post.City,
                SupplierName = post.User!.Username,
                SupplierAvatarUrl = post.User.AvatarUrl,
                CreatedAt = post.CreatedAt,
                ExpiryDate = post.ExpiryDate
            })
            .Where(row => row.RemainingQuantity > row.ReservedQuantity)
            .OrderByDescending(row => row.CreatedAt)
            .ThenBy(row => row.SellPostId)
            .Take(candidateLimit)
            .ToListAsync(cancellationToken);

        if (allowedPostIds is null && rows.Count == candidateLimit)
            logger.LogWarning(
                "Supplier candidate query reached limit {CandidateLimit}; matching may omit older candidates",
                candidateLimit);

        if (rows.Count == 0)
            return [];

        var productIds = rows.Select(row => row.ProductId).ToArray();
        var attributes = await db.Product_Attribute_Values.AsNoTracking()
            .Where(value => productIds.Contains(value.ProductId))
            .Select(value => new
            {
                value.ProductId,
                Attribute = new SupplierCandidateAttribute(
                    value.AttributeId,
                    value.OptionId,
                    value.ValueBoolean,
                    value.ValueText,
                    value.ValueNumber)
            })
            .ToListAsync(cancellationToken);
        var attributesByProduct = attributes
            .GroupBy(value => value.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SupplierCandidateAttribute>)group
                    .Select(value => value.Attribute)
                    .ToArray());

        var supplierIds = rows.Select(row => row.SupplierId).Distinct().ToArray();
        var ratings = await db.Reviews.AsNoTracking()
            .Where(review => supplierIds.Contains(review.RevieweeId) &&
                review.Rating.HasValue &&
                (review.ReviewStatus == (int)ReviewStatus.Active ||
                 review.ReviewStatus == (int)ReviewStatus.Edited))
            .GroupBy(review => review.RevieweeId)
            .Select(group => new
            {
                SupplierId = group.Key,
                Average = group.Average(review => review.Rating!.Value),
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.SupplierId, cancellationToken);

        return rows.Select(row =>
        {
            ratings.TryGetValue(row.SupplierId, out var rating);
            attributesByProduct.TryGetValue(row.ProductId, out var productAttributes);
            return new SupplierCandidate(
                row.SellPostId,
                row.SupplierId,
                row.ProductId,
                row.CategoryId,
                row.ProductTypeId,
                row.BrandId,
                row.ProductName,
                row.CategoryName,
                row.ProductTypeName,
                row.BrandName,
                row.ModelNumber,
                ModelNumberNormalizer.Normalize(row.ModelNumber),
                row.FunctionalityStatus.HasValue
                    ? (FunctionalityStatus?)row.FunctionalityStatus.Value
                    : null,
                row.DamageLevel.HasValue ? (DamageLevel?)row.DamageLevel.Value : null,
                row.UsageDuration,
                row.Price,
                row.RemainingQuantity,
                row.ReservedQuantity,
                row.RemainingQuantity - row.ReservedQuantity,
                string.IsNullOrWhiteSpace(row.City) ? null : row.City.Trim().ToLowerInvariant(),
                row.SupplierName,
                row.SupplierAvatarUrl,
                rating?.Average,
                rating?.Count ?? 0,
                row.CreatedAt,
                row.ExpiryDate,
                productAttributes ?? []);
        }).ToArray();
    }

    private sealed class CandidateRow
    {
        public Guid SellPostId { get; init; }
        public Guid SupplierId { get; init; }
        public Guid ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public Guid? ProductTypeId { get; init; }
        public Guid? BrandId { get; init; }
        public string? ProductName { get; init; }
        public string? CategoryName { get; init; }
        public string? ProductTypeName { get; init; }
        public string? BrandName { get; init; }
        public string? ModelNumber { get; init; }
        public int? FunctionalityStatus { get; init; }
        public int? DamageLevel { get; init; }
        public int? UsageDuration { get; init; }
        public decimal? Price { get; init; }
        public int RemainingQuantity { get; init; }
        public int ReservedQuantity { get; init; }
        public string? City { get; init; }
        public string SupplierName { get; init; } = string.Empty;
        public string? SupplierAvatarUrl { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? ExpiryDate { get; init; }
    }
}
