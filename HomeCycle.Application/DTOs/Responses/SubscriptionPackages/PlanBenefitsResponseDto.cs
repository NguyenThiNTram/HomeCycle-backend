using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages;

public sealed class PlanBenefitsResponseDto
{
    public string Tier { get; set; } = "FREE";
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole Role { get; set; }
    public Guid? SubscriptionId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public PlanAiUsageResponseDto Ai { get; set; } = new();
    public SupplierMatchingBenefitsResponseDto? SupplierMatching { get; set; }
}

public sealed class PlanAiUsageResponseDto
{
    public string Feature { get; set; } = string.Empty;
    public int DailyLimit { get; set; }
    public int UsedToday { get; set; }
    public int RemainingToday { get; set; }
    public DateTimeOffset ResetsAt { get; set; }
}

public sealed class SupplierMatchingBenefitsResponseDto
{
    public int ResultLimit { get; set; }
    public bool AiRerankingEnabled { get; set; }
    public bool AdvancedFiltersEnabled { get; set; }
    public bool DetailedReasonsEnabled { get; set; }
    public bool NewSupplierNotificationsEnabled { get; set; }
}

public sealed class PlanDefinitionResponseDto
{
    public string Tier { get; set; } = "FREE";
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole Role { get; set; }
    public string AiFeature { get; set; } = string.Empty;
    public int AiDailyLimit { get; set; }
    public SupplierMatchingBenefitsResponseDto? SupplierMatching { get; set; }
}
