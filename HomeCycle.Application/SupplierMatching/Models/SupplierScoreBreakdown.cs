namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierScoreBreakdown(
    decimal ProductClassification,
    decimal DynamicAttributes,
    decimal BrandAndModel,
    decimal Budget,
    decimal Quantity,
    decimal Condition,
    decimal City,
    decimal SupplierRating,
    decimal EarnedWeight,
    decimal ApplicableWeight);
