using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages;

public sealed class PlanBenefitsResponseDto
{
    public bool PostingPriorityEnabled { get; set; }
    // SUBSCRIPTION_INACTIVE | PROFILE_REQUIRED | BUSINESS_PROFILE_PENDING | BUSINESS_PROFILE_REJECTED | ACCOUNT_INACTIVE; null khi đã đủ điều kiện.
    public string? PostingPriorityBlockedReason { get; set; }
    public string PostingPriorityDescription => PostingPriorityEnabled
        ? "Bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật, được chọn theo uy tín và thời gian đăng; tối đa 2 bài mỗi chủ trong mỗi danh sách 10 bài."
        : PostingPriorityBlockedReason switch
        {
            "PROFILE_REQUIRED" => "Hoàn tất hồ sơ để bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật.",
            "BUSINESS_PROFILE_PENDING" => "Hồ sơ doanh nghiệp đang chờ phê duyệt. Bài đăng sẽ đủ điều kiện xuất hiện trong khu vực nổi bật sau khi hồ sơ được duyệt.",
            "BUSINESS_PROFILE_REJECTED" => "Hồ sơ doanh nghiệp chưa được phê duyệt. Vui lòng cập nhật hồ sơ để bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật.",
            "ACCOUNT_INACTIVE" => "Tài khoản của bạn hiện không hoạt động nên bài đăng không được ưu tiên.",
            _ => "Đăng ký gói để bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật."
        };
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
    public bool PostingPriorityEnabled { get; set; }
    public string PostingPriorityDescription => PostingPriorityEnabled
        ? "Bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật, được chọn theo uy tín và thời gian đăng; tối đa 2 bài mỗi chủ trong mỗi danh sách 10 bài."
        : "Đăng ký gói để bài đăng đủ điều kiện xuất hiện trong khu vực nổi bật.";
    public string Tier { get; set; } = "FREE";
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole Role { get; set; }
    public string AiFeature { get; set; } = string.Empty;
    public int AiDailyLimit { get; set; }
    public SupplierMatchingBenefitsResponseDto? SupplierMatching { get; set; }
}
