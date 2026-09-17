using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierCandidate(
    Guid SellPostId,
    Guid SupplierId,
    Guid ProductId,
    Guid? CategoryId,
    Guid? ProductTypeId,
    Guid? BrandId,
    string? ProductName,
    string? CategoryName,
    string? ProductTypeName,
    string? BrandName,
    string? ModelNumber,
    string? NormalizedModelNumber,
    FunctionalityStatus? FunctionalityStatus,
    DamageLevel? DamageLevel,
    int? UsageDuration,
    decimal? Price,
    int RemainingQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    string? City,
    string SupplierName,
    string? SupplierAvatarUrl,
    double? AverageRating,
    int TotalReviews,
    DateTime CreatedAt,
    DateTime? ExpiryDate,
    IReadOnlyList<SupplierCandidateAttribute> Attributes);
