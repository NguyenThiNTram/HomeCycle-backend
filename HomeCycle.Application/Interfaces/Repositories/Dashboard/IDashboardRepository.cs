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
    Task<OperationOverviewData> GetOperationOverviewAsync(DashboardPeriod period, DateTime nowUtc, CancellationToken ct);
    Task<PaymentDashboardData> GetPaymentsAsync(PaymentDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct);
    Task<OrderDashboardData> GetOrdersAsync(OrderDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct);
    Task<AppointmentDashboardData> GetAppointmentsAsync(AppointmentDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct);
    Task<DisputeDashboardData> GetDisputesAsync(DisputeDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct);
    Task<BusinessOverviewResponse> GetBusinessOverviewAsync(BusinessOverviewRequest request, CancellationToken ct);
    Task<BusinessDemandResponse> GetBusinessDemandAsync(BusinessDemandRequest request, CancellationToken ct);
    Task<BusinessPerformanceData> GetBusinessPerformanceAsync(DashboardPeriod period, CancellationToken ct);
    Task<FinanceOverviewData> GetFinanceOverviewAsync(
        DashboardPeriod period,
        CancellationToken ct);
    Task<FinanceCashFlowData> GetFinanceCashFlowAsync(
        DashboardPeriod period,
        CancellationToken ct);
    Task<FinancePaymentStatusData> GetFinancePaymentStatusAsync(
        DashboardPeriod period,
        CancellationToken ct);
    Task<PagedResult<FinanceTransactionItem>> GetFinanceTransactionsAsync(
        FinanceTransactionRequest request,
        DashboardPeriod period,
        CancellationToken ct);
    Task<FinanceHealthData> GetFinanceHealthAsync(
        DashboardPeriod period,
        DateTime nowUtc,
        CancellationToken ct);
}
