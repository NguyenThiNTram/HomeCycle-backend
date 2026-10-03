using System.Globalization;
using HomeCycle.Application.Interfaces.Repositories.AI;
using HomeCycle.Application.Pricing.Models;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure.Repositories.AI;

public sealed class PriceEvidenceRepository(HomeCycleDbContext db) : IPriceEvidenceRepository
{
    private const int CandidateLimit = 200;

    public async Task<IReadOnlyList<CompletedPriceEvidence>> GetCompletedAsync(
        Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken)
    {
        Validate(typeId, brandId, model, nowUtc);
        var since = nowUtc.AddDays(-90);
        var candidates = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == (int)OrderStatus.Completed && x.ReturnedAt == null &&
                x.CompletedAt >= since && x.CompletedAt <= nowUtc && x.Agreement.FinalPrice > 0 &&
                x.Post.PostType == (int)PostType.Sell && x.Post.Product != null &&
                x.Post.Product.ProductTypeId == typeId && x.Post.Product.BrandId == brandId &&
                PostgresPricingFunctions.RegexpReplace((x.Post.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") == model)
            .OrderByDescending(x => x.CompletedAt).ThenBy(x => x.OrderId)
            .Select(x => new CompletedCandidate(x.Post.Product!.ProductId,
                PostgresPricingFunctions.RegexpReplace(
                    (x.Post.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g"),
                x.Post.Product.FunctionalityStatus, x.Post.Product.DamageLevel,
                x.CompletedAt!.Value, x.Agreement.FinalPrice!.Value))
            .Take(CandidateLimit).ToListAsync(cancellationToken);

        var attributes = await GetAttributesAsync(candidates.Select(x => x.ProductId), cancellationToken);
        return candidates.Select(x => new CompletedPriceEvidence(
            x.ProductId, x.NormalizedModelNumber, x.FunctionalityStatus, x.DamageLevel,
            x.CompletedAt, x.Price,
            attributes.GetValueOrDefault(x.ProductId) ?? [])).ToArray();
    }

    public async Task<IReadOnlyList<ListingPriceEvidence>> GetActiveAsync(
        Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken)
    {
        Validate(typeId, brandId, model, nowUtc);
        var candidates = await db.Posts.AsNoTracking()
            .Where(x => x.PostType == (int)PostType.Sell && x.Status == (int)PostStatus.Active &&
                x.BasePrice > 0 && x.RemainingQuantity > 0 && (x.ExpiryDate == null || x.ExpiryDate > nowUtc) &&
                x.Product != null && x.Product.ProductTypeId == typeId && x.Product.BrandId == brandId &&
                PostgresPricingFunctions.RegexpReplace((x.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") == model)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PostId)
            .Select(x => new ListingCandidate(
                x.Product!.ProductId,
                PostgresPricingFunctions.RegexpReplace(
                    (x.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g"),
                x.Product.FunctionalityStatus,
                x.Product.DamageLevel,
                x.BasePrice!.Value))
            .Take(CandidateLimit).ToListAsync(cancellationToken);

        var attributes = await GetAttributesAsync(candidates.Select(x => x.ProductId), cancellationToken);
        return candidates.Select(x => new ListingPriceEvidence(
            x.ProductId, x.NormalizedModelNumber, x.FunctionalityStatus, x.DamageLevel,
            x.Price, attributes.GetValueOrDefault(x.ProductId) ?? [])).ToArray();
    }

    public async Task<IReadOnlyList<CompletedPriceEvidence>> GetEquivalentCompletedAsync(
        Guid typeId,
        Guid brandId,
        string excludedModel,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        Validate(typeId, brandId, excludedModel, nowUtc);
        var since = nowUtc.AddDays(-90);
        var candidates = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == (int)OrderStatus.Completed && x.ReturnedAt == null &&
                x.CompletedAt >= since && x.CompletedAt <= nowUtc && x.Agreement.FinalPrice > 0 &&
                x.Post.PostType == (int)PostType.Sell && x.Post.Product != null &&
                x.Post.Product.ProductTypeId == typeId && x.Post.Product.BrandId == brandId &&
                PostgresPricingFunctions.RegexpReplace(
                    (x.Post.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") != "" &&
                PostgresPricingFunctions.RegexpReplace(
                    (x.Post.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") != excludedModel)
            .OrderByDescending(x => x.CompletedAt).ThenBy(x => x.OrderId)
            .Select(x => new CompletedCandidate(
                x.Post.Product!.ProductId,
                PostgresPricingFunctions.RegexpReplace(
                    (x.Post.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g"),
                x.Post.Product.FunctionalityStatus,
                x.Post.Product.DamageLevel,
                x.CompletedAt!.Value,
                x.Agreement.FinalPrice!.Value))
            .Take(CandidateLimit)
            .ToListAsync(cancellationToken);

        var attributes = await GetAttributesAsync(candidates.Select(x => x.ProductId), cancellationToken);
        return candidates.Select(x => new CompletedPriceEvidence(
            x.ProductId,
            x.NormalizedModelNumber,
            x.FunctionalityStatus,
            x.DamageLevel,
            x.CompletedAt,
            x.Price,
            attributes.GetValueOrDefault(x.ProductId) ?? [])).ToArray();
    }

    public async Task<IReadOnlyList<ListingPriceEvidence>> GetEquivalentActiveAsync(
        Guid typeId,
        Guid brandId,
        string excludedModel,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        Validate(typeId, brandId, excludedModel, nowUtc);
        var candidates = await db.Posts.AsNoTracking()
            .Where(x => x.PostType == (int)PostType.Sell && x.Status == (int)PostStatus.Active &&
                x.BasePrice > 0 && x.RemainingQuantity > 0 &&
                (x.ExpiryDate == null || x.ExpiryDate > nowUtc) &&
                x.Product != null && x.Product.ProductTypeId == typeId && x.Product.BrandId == brandId &&
                PostgresPricingFunctions.RegexpReplace(
                    (x.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") != "" &&
                PostgresPricingFunctions.RegexpReplace(
                    (x.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g") != excludedModel)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PostId)
            .Select(x => new ListingCandidate(
                x.Product!.ProductId,
                PostgresPricingFunctions.RegexpReplace(
                    (x.Product.ModelNumber ?? "").ToLower(), "[^a-z0-9]", "", "g"),
                x.Product.FunctionalityStatus,
                x.Product.DamageLevel,
                x.BasePrice!.Value))
            .Take(CandidateLimit)
            .ToListAsync(cancellationToken);

        var attributes = await GetAttributesAsync(candidates.Select(x => x.ProductId), cancellationToken);
        return candidates.Select(x => new ListingPriceEvidence(
            x.ProductId,
            x.NormalizedModelNumber,
            x.FunctionalityStatus,
            x.DamageLevel,
            x.Price,
            attributes.GetValueOrDefault(x.ProductId) ?? [])).ToArray();
    }

    public async Task<IReadOnlyList<MarketPriceEvidence>> GetVerifiedMarketAsync(
        Guid typeId, Guid brandId, string model, DateTime nowUtc, CancellationToken cancellationToken)
    {
        Validate(typeId, brandId, model, nowUtc);
        var since = nowUtc.AddDays(-180);
        return await db.MarketPriceReferences.AsNoTracking()
            .Where(x => x.ProductTypeId == typeId && x.BrandId == brandId &&
                x.NormalizedModelNumber == model && x.IsVerified && x.PriceVndPerUnit > 0 &&
                x.ObservedAt >= since && x.ObservedAt <= nowUtc)
            .OrderByDescending(x => x.ObservedAt).ThenBy(x => x.Id)
            .Select(x => new MarketPriceEvidence(x.PriceVndPerUnit, x.SourceName, x.SourceUrl,
                x.ObservedAt, x.HasVariants, x.KeySpecifications))
            .Take(10).ToListAsync(cancellationToken);
    }

    private static void Validate(Guid typeId, Guid brandId, string model, DateTime nowUtc)
    {
        if (typeId == Guid.Empty || brandId == Guid.Empty || string.IsNullOrEmpty(model) ||
            model.Length > 100 || model.Any(c => !char.IsAsciiLetterOrDigit(c) || char.IsUpper(c)))
            throw new ArgumentException("Evidence lookup requires type, brand and a normalized model.");
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Evidence lookup requires UTC timestamps.", nameof(nowUtc));
    }

    private async Task<Dictionary<Guid, IReadOnlyList<DynamicAttributeValue>>> GetAttributesAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var rows = await db.Product_Attribute_Values.AsNoTracking()
            .Where(x => ids.Contains(x.ProductId))
            .Select(x => new AttributeRow(
                x.ProductId,
                x.AttributeId,
                x.Attribute.AttributeName,
                x.Attribute.DataType,
                x.Attribute.Unit,
                x.OptionId,
                x.Option != null ? x.Option.OptionValue : null,
                x.ValueNumber,
                x.ValueBoolean,
                x.ValueText,
                x.Attribute.IsRequired,
                x.Attribute.IsFilterable,
                x.Attribute.DisplayOrder))
            .ToListAsync(cancellationToken);

        return rows
            .Where(x => x.DataType.HasValue && Enum.IsDefined(typeof(DataType), x.DataType.Value))
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DynamicAttributeValue>)group
                    .Select(x => new DynamicAttributeValue(
                        x.AttributeId,
                        x.AttributeName?.Trim() ?? x.AttributeId.ToString(),
                        (DataType)x.DataType!.Value,
                        string.IsNullOrWhiteSpace(x.Unit) ? null : x.Unit.Trim(),
                        x.OptionId,
                        x.OptionValue?.Trim() ??
                            x.ValueNumber?.ToString(CultureInfo.InvariantCulture) ??
                            (x.ValueBoolean.HasValue
                                ? x.ValueBoolean.Value.ToString().ToLowerInvariant()
                                : x.ValueText?.Trim()),
                        x.ValueNumber,
                        x.ValueBoolean,
                        x.ValueText?.Trim(),
                        x.IsRequired,
                        x.IsFilterable,
                        x.DisplayOrder ?? int.MaxValue))
                    .ToArray());
    }

    private sealed record CompletedCandidate(
        Guid ProductId,
        string NormalizedModelNumber,
        int? FunctionalityStatus,
        int? DamageLevel,
        DateTime CompletedAt,
        decimal Price);

    private sealed record ListingCandidate(
        Guid ProductId,
        string NormalizedModelNumber,
        int? FunctionalityStatus,
        int? DamageLevel,
        decimal Price);

    private sealed record AttributeRow(
        Guid ProductId,
        Guid AttributeId,
        string? AttributeName,
        int? DataType,
        string? Unit,
        Guid? OptionId,
        string? OptionValue,
        decimal? ValueNumber,
        bool? ValueBoolean,
        string? ValueText,
        bool IsRequired,
        bool IsFilterable,
        int? DisplayOrder);
}
