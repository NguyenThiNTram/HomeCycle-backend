using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Interfaces.Services.Dashboard;

public interface IDashboardService
{
    Task<UserDashboardOverviewResponse> GetUserOverviewAsync(UserRole? role, CancellationToken ct);
    Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct);
    Task<UserRegistrationTrendResponse> GetUserRegistrationTrendAsync(UserRegistrationTrendRequest request, CancellationToken ct);
    Task<OperationOverviewResponse> GetOperationOverviewAsync(DashboardPeriodRequest request, CancellationToken ct);
    Task<PaymentDashboardResponse> GetPaymentDashboardAsync(PaymentDashboardRequest request, CancellationToken ct);
    Task<OrderDashboardResponse> GetOrderDashboardAsync(OrderDashboardRequest request, CancellationToken ct);
    Task<AppointmentDashboardResponse> GetAppointmentDashboardAsync(AppointmentDashboardRequest request, CancellationToken ct);
    Task<DisputeDashboardResponse> GetDisputeDashboardAsync(DisputeDashboardRequest request, CancellationToken ct);
    Task<BusinessOverviewResponse> GetBusinessOverviewAsync(BusinessOverviewRequest request, CancellationToken ct);
    Task<BusinessDemandResponse> GetBusinessDemandAsync(BusinessDemandRequest request, CancellationToken ct);
    Task<BusinessPerformanceResponse> GetBusinessPerformanceAsync(DashboardPeriodRequest request, CancellationToken ct);
    Task<FinanceOverviewResponse> GetFinanceOverviewAsync(
        DashboardPeriodRequest request,
        CancellationToken ct);
    Task<FinanceCashFlowResponse> GetFinanceCashFlowAsync(
        DashboardPeriodRequest request,
        CancellationToken ct);
    Task<FinancePaymentStatusResponse> GetFinancePaymentStatusAsync(
        DashboardPeriodRequest request,
        CancellationToken ct);
    Task<PagedResult<FinanceTransactionItem>> GetFinanceTransactionsAsync(
        FinanceTransactionRequest request,
        CancellationToken ct);
    Task<FinanceHealthResponse> GetFinanceHealthAsync(
        DashboardPeriodRequest request,
        CancellationToken ct);
}
