namespace HomeCycle.Application.SupplierMatching.Models;

public enum SupplierMatchLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum SupplierCriterionState
{
    NotSpecified = 0,
    Unknown = 1,
    Conflicted = 2,
    Matched = 3
}

public enum SupplierModelMatchLevel
{
    NotSpecified = 0,
    Unknown = 1,
    Unrelated = 2,
    Related = 3,
    Variant = 4,
    Exact = 5
}
