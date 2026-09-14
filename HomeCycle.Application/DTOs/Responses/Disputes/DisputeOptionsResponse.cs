using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.Disputes;

public sealed class DisputeOptionsResponse
{
    public IReadOnlyList<DisputeTargetOptionResponse> TargetTypes { get; set; } = [];
    public int MinimumEvidenceImages { get; set; } = 2;
    public int MaximumEvidenceImages { get; set; } = 5;
    public int MinimumDescriptionLength { get; set; } = 10;
    public int MaximumDescriptionLength { get; set; } = 2000;
}

public sealed class DisputeTargetOptionResponse
{
    public DisputeTargetType Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<DisputeCategoryOptionResponse> Categories { get; set; } = [];
}

public sealed class DisputeCategoryOptionResponse
{
    public DisputeCategory Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
