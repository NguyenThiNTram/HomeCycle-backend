using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Repositories.Dashboard;
using HomeCycle.Application.Interfaces.Services.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Dashboard;

public sealed class UserDashboardService(IUserDashboardRepository repository, TimeProvider clock) : IUserDashboardService
{
    public async Task<UserDashboardOverviewResponse> GetOverviewAsync(UserRole? role, CancellationToken ct)
    {
        var groups = await repository.GetAccountGroupsAsync(role, ct);
        return new UserDashboardOverviewResponse
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime,
            Role = role,
            TotalAccounts = groups.Sum(x => x.Count),
            EmailVerifiedAccounts = groups.Where(x => x.IsEmailVerified).Sum(x => x.Count),
            ByStatus = Enum.GetValues<UserStatus>()
                .Select(status => new UserStatusCount(status, groups.Where(x => x.Status == status).Sum(x => x.Count))).ToArray(),
            ByRole = Enum.GetValues<UserRole>()
                .Select(value => new UserRoleCount(value, groups.Where(x => x.Role == value).Sum(x => x.Count))).ToArray()
        };
    }

    public Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct)
        => repository.GetUsersAsync(request, ct);

    public async Task<UserRegistrationTrendResponse> GetRegistrationTrendAsync(UserRegistrationTrendRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var endUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
        var registrations = await repository.GetRegistrationsAsync(request.Role, endUtc.AddDays(-2 * request.Days), endUtc, ct);
        return UserRegistrationTrendCalculator.Calculate(registrations, today, request.Days, request.ForecastDays, request.Role, now.UtcDateTime);
    }
}
