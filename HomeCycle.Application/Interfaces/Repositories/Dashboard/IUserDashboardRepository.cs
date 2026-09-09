using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Interfaces.Repositories.Dashboard;

public interface IUserDashboardRepository
{
    Task<IReadOnlyList<UserAccountGroup>> GetAccountGroupsAsync(UserRole? role, CancellationToken ct);
    Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct);
    Task<IReadOnlyList<RegistrationDay>> GetRegistrationsAsync(UserRole? role, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
