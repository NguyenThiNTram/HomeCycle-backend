using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Pricing.Models;

public sealed record DynamicProductContext(
    Guid ProductTypeId,
    string ProductTypeName,
    Guid BrandId,
    string BrandName,
    string ProductName,
    string Model,
    FunctionalityStatus FunctionalityStatus,
    DamageLevel DamageLevel,
    int? UsageDuration,
    IReadOnlyList<DynamicAttributeValue> Attributes);

public sealed record DynamicAttributeValue(
    Guid AttributeId,
    string Name,
    DataType DataType,
    string? Unit,
    Guid? OptionId,
    string? DisplayValue,
    decimal? NumberValue,
    bool? BooleanValue,
    string? TextValue,
    bool IsRequired,
    bool IsFilterable,
    int DisplayOrder);
