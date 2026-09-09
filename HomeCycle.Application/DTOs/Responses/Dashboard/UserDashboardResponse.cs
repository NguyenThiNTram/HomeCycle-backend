using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed record UserStatusCount(UserStatus Status, int Count);
public sealed record UserRoleCount(UserRole Role, int Count);
public sealed record RegistrationDay(DateOnly Date, int Count);
public sealed record UserAccountGroup(UserRole Role, UserStatus Status, bool IsEmailVerified, int Count);

public sealed class UserDashboardOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public UserRole? Role { get; init; }
    public int TotalAccounts { get; init; }
    public int EmailVerifiedAccounts { get; init; }
    public IReadOnlyList<UserStatusCount> ByStatus { get; init; } = [];
    public IReadOnlyList<UserRoleCount> ByRole { get; init; } = [];
}

public sealed class UserRegistrationTrendResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public string TimeZone { get; init; } = "Asia/Ho_Chi_Minh";
    public UserRole? Role { get; init; }
    public DateOnly PreviousPeriodStart { get; init; }
    public DateOnly CurrentPeriodStart { get; init; }
    public DateOnly CurrentPeriodEndExclusive { get; init; }
    public int PreviousPeriodRegistrations { get; init; }
    public int CurrentPeriodRegistrations { get; init; }
    public int RegistrationChange { get; init; }
    // Null when the previous period is zero: percentage growth is undefined.
    public decimal? GrowthPercent { get; init; }
    public decimal AverageDailyRegistrations { get; init; }
    public string Direction { get; init; } = "Stable";
    public IReadOnlyList<RegistrationDay> DailyRegistrations { get; init; } = [];
    public RegistrationForecast Forecast { get; init; } = new();
}

public sealed class RegistrationForecast
{
    public string Method { get; init; } = "TrailingDailyAverage";
    public bool IsEstimate { get; init; } = true;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDateExclusive { get; init; }
    public int TrainingDays { get; init; }
    public int ObservedRegistrations { get; init; }
    public int Days { get; init; }
    public decimal EstimatedRegistrations { get; init; }
    public string Limitation { get; init; } = "Ước tính từ trung bình kỳ gần nhất; không mô hình hóa mùa vụ hoặc chiến dịch marketing. Ngày không có đăng ký được tính bằng 0.";
}
