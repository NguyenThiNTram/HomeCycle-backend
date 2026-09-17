using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierDemandContext(
    Guid? RequesterId,
    Guid? CategoryId,
    Guid? ProductTypeId,
    Guid? BrandId,
    string? ModelNumber,
    string? NormalizedModelNumber,
    FunctionalityStatus? FunctionalityStatus,
    DamageLevel? DamageLevel,
    int? UsageDuration,
    decimal? PriceFrom,
    decimal? PriceTo,
    int Quantity,
    string? City,
    IReadOnlyList<SupplierCandidateAttribute> Attributes,
    SupplierMatchAdvancedFilters AdvancedFilters);

public sealed record SupplierMatchAdvancedFilters(
    bool RequireFullQuantity,
    bool StrictBudget,
    bool StrictBrand,
    bool SameCityOnly,
    double? MinimumSellerRating)
{
    public static SupplierMatchAdvancedFilters None { get; } = new(false, false, false, false, null);
}
