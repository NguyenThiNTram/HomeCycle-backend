using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Interfaces.Repositories.Dashboard;

public interface IDashboardRepository
{
    Task<IReadOnlyList<UserAccountGroup>> GetAccountGroupsAsync(UserRole? role, CancellationToken ct);
    Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct);
    Task<IReadOnlyList<RegistrationDay>> GetRegistrationsAsync(UserRole? role, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<OperationDashboardData> GetPaymentsAsync(PaymentDashboardRequest request, DashboardPeriod period, CancellationToken ct);
    Task<OperationDashboardData> GetOrdersAsync(OrderDashboardRequest request, DashboardPeriod period, CancellationToken ct);
    Task<OperationDashboardData> GetAppointmentsAsync(AppointmentDashboardRequest request, DashboardPeriod period, CancellationToken ct);
    Task<OperationDashboardData> GetDisputesAsync(DisputeDashboardRequest request, DashboardPeriod period, CancellationToken ct);
    Task<BusinessOverviewResponse> GetBusinessOverviewAsync(BusinessOverviewRequest request, CancellationToken ct);
    Task<BusinessDemandResponse> GetBusinessDemandAsync(BusinessDemandRequest request, CancellationToken ct);
    Task<BusinessPerformanceData> GetBusinessPerformanceAsync(DashboardPeriod period, CancellationToken ct);
}
