using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Application.SupplierMatching.Normalization;
using HomeCycle.Domain.Entities;

namespace HomeCycle.Application.SupplierMatching.Services;

public static class SupplierDemandContextBuilder
{
    public static SupplierDemandContext FromDraft(Guid requesterId, SupplierMatchDraftRequest request) => new(
        requesterId,
        request.CategoryId,
        request.ProductTypeId,
        request.BrandId,
        CleanOptional(request.ModelNumber),
        ModelNumberNormalizer.Normalize(request.ModelNumber),
        request.FunctionalityStatus,
        request.DamageLevel,
        request.UsageDuration,
        request.PriceFrom,
        request.PriceTo,
        request.Quantity,
        NormalizeCity(request.City),
        request.AttributeValues.Select(x => new SupplierCandidateAttribute(
            x.AttributeId,
            x.OptionId,
            x.ValueBoolean,
            CleanOptional(x.ValueText),
            x.ValueNumber)).ToArray(),
        request.AdvancedFilters is null
            ? SupplierMatchAdvancedFilters.None
            : new SupplierMatchAdvancedFilters(
                request.AdvancedFilters.RequireFullQuantity,
                request.AdvancedFilters.StrictBudget,
                request.AdvancedFilters.StrictBrand,
                request.AdvancedFilters.SameCityOnly,
                request.AdvancedFilters.MinimumSellerRating));

    public static SupplierDemandContext FromBuyPost(post buyPost)
    {
        ArgumentNullException.ThrowIfNull(buyPost.Product);
        var product = buyPost.Product;
        return new SupplierDemandContext(
            buyPost.OwnerId,
            product.CategoryId,
            product.ProductTypeId,
            product.BrandId,
            CleanOptional(product.ModelNumber),
            ModelNumberNormalizer.Normalize(product.ModelNumber),
            product.FunctionalityStatus,
            product.DamageLevel,
            product.UsageDuration,
            buyPost.MinExpectedPrice,
            buyPost.BasePrice,
            Math.Max(1, buyPost.RemainingQuantity),
            NormalizeCity(buyPost.City),
            product.Product_Attribute_Values.Select(x => new SupplierCandidateAttribute(
                x.AttributeId,
                x.OptionId,
                x.ValueBoolean,
                CleanOptional(x.ValueText),
                x.ValueNumber)).ToArray(),
            SupplierMatchAdvancedFilters.None);
    }

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCity(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
