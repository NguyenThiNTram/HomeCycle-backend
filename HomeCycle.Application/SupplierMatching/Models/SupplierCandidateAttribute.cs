namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierCandidateAttribute(
    Guid AttributeId,
    Guid? OptionId,
    bool? BooleanValue,
    string? TextValue,
    decimal? NumberValue);
