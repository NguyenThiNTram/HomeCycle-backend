using System.ComponentModel.DataAnnotations;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.Dashboard;

public class UserDashboardScopeRequest
{
    [EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; set; }
}

public sealed class DashboardUserListRequest : UserDashboardScopeRequest
{
    [EnumDataType(typeof(UserStatus))]
    public UserStatus? Status { get; set; }

    [StringLength(200)]
    public string? Keyword { get; set; }

    [Range(1, 1000000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public sealed class UserRegistrationTrendRequest : UserDashboardScopeRequest
{
    // Two consecutive periods of this length, excluding today's incomplete data.
    [Range(7, 90)]
    public int Days { get; set; } = 30;

    [Range(1, 30)]
    public int ForecastDays { get; set; } = 7;
}
