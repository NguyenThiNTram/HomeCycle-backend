using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Interfaces.Services.Dashboard;

public interface IUserDashboardService
{
    Task<UserDashboardOverviewResponse> GetOverviewAsync(UserRole? role, CancellationToken ct);
    Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct);
    Task<UserRegistrationTrendResponse> GetRegistrationTrendAsync(UserRegistrationTrendRequest request, CancellationToken ct);
}
