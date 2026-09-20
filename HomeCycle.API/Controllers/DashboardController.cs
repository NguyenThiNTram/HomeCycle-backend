using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Services.Dashboard;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Payments;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet("listings")]
    public async Task<ActionResult<ListingDashboardResponse>> GetListings([FromQuery] DashboardPeriodRequest request, CancellationToken ct)
        => Ok(await service.GetListingDashboardAsync(request, ct));

    [HttpGet("listings/reported")]
    public async Task<ActionResult<PagedResult<ReportedListingItem>>> GetReportedListings([FromQuery] ReportedListingRequest request, CancellationToken ct)
        => Ok(await service.GetReportedListingsAsync(request, ct));

    [HttpGet("subscription-packages")]
    public async Task<ActionResult<SubscriptionDashboardResponse>> GetSubscriptions([FromQuery] DashboardPeriodRequest request, CancellationToken ct)
        => Ok(await service.GetSubscriptionDashboardAsync(request, ct));

    [HttpGet("users/activity")]
    public async Task<ActionResult<UserActivityResponse>> GetUserActivity(CancellationToken ct)
        => Ok(await service.GetUserActivityAsync(ct));

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<DashboardAccountDetail>> GetUserDetail(Guid userId, CancellationToken ct)
    {
        var detail = await service.GetAccountDetailAsync(userId, ct);
        return detail == null ? NotFound() : Ok(detail);
    }

    [HttpGet("orders/history")]
    public async Task<IActionResult> GetOrderHistory([FromQuery] ModeratorOrderSearchRequest request, [FromServices] IOrderService orders, CancellationToken ct)
        => ReadResult(await orders.GetAllForModeratorAsync(request, ct));

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrderDetail(Guid orderId, [FromServices] IOrderService orders, CancellationToken ct)
        => ReadResult(await orders.GetDetailForModeratorAsync(orderId, ct));

    [HttpGet("orders/{orderId:guid}/financial-history")]
    public async Task<IActionResult> GetOrderFinancialHistory(Guid orderId, [FromServices] IPaymentService payments, CancellationToken ct)
        => ReadResult(await payments.GetOrderFinancialHistoryForModeratorAsync(orderId, ct));

    [HttpGet("appointments/history")]
    public async Task<IActionResult> GetAppointmentHistory([FromQuery] ModeratorAppointmentSearchRequest request, [FromServices] IAppointmentService appointments, CancellationToken ct)
        => ReadResult(await appointments.GetAllForModeratorAsync(request, ct));

    [HttpGet("appointments/{appointmentId:guid}")]
    public async Task<IActionResult> GetAppointmentDetail(Guid appointmentId, [FromServices] IAppointmentService appointments, CancellationToken ct)
        => ReadResult(await appointments.GetDetailForModeratorAsync(appointmentId, ct));

    [HttpGet("disputes/history")]
    public async Task<IActionResult> GetDisputeHistory([FromQuery] DisputeSearchRequest request, [FromServices] IDisputeService disputes, CancellationToken ct)
        => ReadResult(await disputes.GetAllForModeratorAsync(request, ct));

    [HttpGet("disputes/{disputeId:guid}")]
    public async Task<IActionResult> GetDisputeDetail(Guid disputeId, [FromServices] IDisputeService disputes, CancellationToken ct)
    {
        var result = await disputes.GetDetailForModeratorAsync(disputeId, Guid.Empty, ct);
        if (result.IsSuccess && result.Data != null) result.Data.Actions = new DisputeActionDto();
        return ReadResult(result);
    }

    private IActionResult ReadResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(result.Data)
            : result.Error?.Code.EndsWith("NOT_FOUND", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(result.Error) : BadRequest(result.Error);

    [HttpGet("users/overview")]
    [SwaggerOperation(Summary = "Tổng quan tài khoản và số lượng theo trạng thái/role",
        Description = "Mặc định bao gồm mọi role và trạng thái Deleted. Role là bộ lọc tùy chọn. Active là trạng thái tài khoản, không phải đang online.")]
    public async Task<ActionResult<UserDashboardOverviewResponse>> GetUserOverview(
        [FromQuery] UserDashboardScopeRequest request, CancellationToken ct)
        => Ok(await service.GetUserOverviewAsync(request.Role, ct));

    [HttpGet("users")]
    [SwaggerOperation(Summary = "Danh sách tài khoản theo trạng thái, role và từ khóa",
        Description = "Phân trang 1–100 bản ghi/trang. TotalCount phản ánh tất cả bộ lọc; sắp xếp ngày tạo giảm dần.")]
    public async Task<ActionResult<PagedResult<UserAdminResponse>>> GetUsers(
        [FromQuery] DashboardUserListRequest request, CancellationToken ct)
        => Ok(await service.GetUsersAsync(request, ct));

    [HttpGet("users/registration-trend")]
    [SwaggerOperation(Summary = "Xu hướng đăng ký mới và ước tính số đăng ký",
        Description = "So sánh hai kỳ liên tiếp Days ngày (7–90, mặc định 30), loại ngày hôm nay chưa hoàn tất, theo UTC+7. Dự đoán ForecastDays ngày (1–30) từ trung bình kỳ gần nhất; không phải cam kết tăng trưởng. Không lọc trạng thái hiện tại để tránh bỏ sót đăng ký đã bị khóa/xóa mềm.")]
    public async Task<ActionResult<UserRegistrationTrendResponse>> GetRegistrationTrend(
        [FromQuery] UserRegistrationTrendRequest request, CancellationToken ct)
        => Ok(await service.GetUserRegistrationTrendAsync(request, ct));

    [HttpGet("operations/overview")]
    [SwaggerOperation(Summary = "Tổng quan nhanh trạng thái vận hành hiện tại",
        Description = "Trả snapshot hiện tại của đơn hàng, lịch hẹn, thanh toán và tranh chấp. From/To chỉ áp dụng cho số tranh chấp đã giải quyết trong kỳ; là ngày UTC+7 và To không tính.")]
    public async Task<ActionResult<OperationOverviewResponse>> GetOperationOverview(
        [FromQuery] DashboardPeriodRequest request, CancellationToken ct)
        => Ok(await service.GetOperationOverviewAsync(request, ct));

    [HttpGet("payments")]
    [SwaggerOperation(Summary = "Thống kê vận hành thanh toán",
        Description = "Tổng và phân bố trạng thái là snapshot hiện tại. Chuỗi Paid và PaidInPeriodCount dùng PaidAt. Tuổi pending tính từ CreatedAt. Tỷ lệ thành công theo phương thức = Paid/(Paid+Failed); null khi chưa có kết quả cuối. Các filter áp dụng cho toàn bộ response.")]
    public async Task<ActionResult<PaymentDashboardResponse>> GetPayments(
        [FromQuery] PaymentDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetPaymentDashboardAsync(request, ct));

    [HttpGet("orders")]
    [SwaggerOperation(Summary = "Thống kê kết quả và tồn đọng đơn hàng",
        Description = "Snapshot và OutcomeSeries cũ được giữ. GMV/AOV dùng đơn hiện Completed theo CompletedAt; AOV null nếu thiếu giá trị. Tỷ lệ hủy/hoàn và hoàn tất dùng trạng thái hiện tại của nhóm đơn tạo trong kỳ. DeliveryMethod dùng shipment mới nhất. PaymentMethodDistribution đếm sự kiện thanh toán cọc/toàn phần đã trả trong kỳ, kể cả đã hoàn tiền; không phải số đơn.")]
    public async Task<ActionResult<OrderDashboardResponse>> GetOrders(
        [FromQuery] OrderDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetOrderDashboardAsync(request, ct));

    [HttpGet("appointments")]
    [SwaggerOperation(Summary = "Báo cáo và giám sát hoạt động lịch hẹn toàn hệ thống",
        Description = "Dashboard read-only loại proposal chưa nhận và lịch bị thay thế. Check-in theo Inspection đủ điều kiện; trễ đối chiếu LateThresholdAt đã lưu. Khu vực dùng City/Ward của bài đăng, hiệu quả theo phương thức shipment mới nhất và trạng thái lịch hiện tại; không suy diễn tỷ lệ Carrier nhận lịch. From/To theo UTC+7, To không tính.")]
    public async Task<ActionResult<AppointmentDashboardResponse>> GetAppointments(
        [FromQuery] AppointmentDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetAppointmentDashboardAsync(request, ct));

    [HttpGet("disputes")]
    [SwaggerOperation(Summary = "Thống kê case, luồng xử lý và tồn đọng tranh chấp",
        Description = "Tổng, unresolved và các phân bố là snapshot hiện tại. Chuỗi opened dùng CreatedAt; resolved và thời gian xử lý dùng ResolvedAt. Category được sắp theo lượt giảm dần để FE lọc hoặc chọn nhiều/ít nhất; Unspecified là dữ liệu không xác định.")]
    public async Task<ActionResult<DisputeDashboardResponse>> GetDisputes(
        [FromQuery] DisputeDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetDisputeDashboardAsync(request, ct));

    [HttpGet("businesses/overview")]
    [SwaggerOperation(Summary = "Tổng quan khách hàng doanh nghiệp",
        Description = "Theo role Business hiện tại, bao gồm Deleted nếu không lọc. Lọc model/profileStatus loại tài khoản chưa có hồ sơ. Survey coverage dùng số có hồ sơ làm mẫu số. GrowthSeries nhóm ngày đăng ký trong kỳ và trạng thái hiện tại; không tái dựng lịch sử Active/Suspended.")]
    public async Task<ActionResult<BusinessOverviewResponse>> GetBusinessOverview(
        [FromQuery] BusinessOverviewRequest request, CancellationToken ct)
        => Ok(await service.GetBusinessOverviewAsync(request, ct));

    [HttpGet("businesses/demand")]
    [SwaggerOperation(Summary = "Nhu cầu khảo sát hiện tại của doanh nghiệp",
        Description = "Đếm distinct doanh nghiệp theo lựa chọn. Mỗi nhóm có mẫu số người trả lời riêng; tổng phần trăm có thể vượt 100%. TargetCity khác ServiceCity. Không phải lịch sử khảo sát.")]
    public async Task<ActionResult<BusinessDemandResponse>> GetBusinessDemand(
        [FromQuery] BusinessDemandRequest request, CancellationToken ct)
        => Ok(await service.GetBusinessDemandAsync(request, ct));

    [HttpGet("businesses/performance")]
    [SwaggerOperation(Summary = "Tỷ trọng giao dịch và giá trị mua/bán doanh nghiệp",
        Description = "Giao dịch: Payment Deposit/Full_Payment đã trả theo PaidAt, gồm đã hoàn tiền. Giá trị: đơn hiện Completed theo CompletedAt, FinalTotalAmount gồm phí ship cấu hình; không phải doanh thu thuần. Role là role hiện tại.")]
    public async Task<ActionResult<BusinessPerformanceResponse>> GetBusinessPerformance(
        [FromQuery] DashboardPeriodRequest request, CancellationToken ct)
        => Ok(await service.GetBusinessPerformanceAsync(request, ct));


    [HttpGet("finance/overview")]
    [SwaggerOperation(
        Summary = "Tổng quan vị thế và hoạt động tài chính",
        Description =
            "Position là snapshot số dư hiện tại. Activity là dòng tiền trong khoảng From/To. " +
            "ExternalInflow chỉ tính PayOS đã thanh toán; ExternalOutflow hiện chỉ tính Withdrawal_Success. " +
            "Refund và release là dịch chuyển nội bộ, không phải external outflow.")]
    public async Task<ActionResult<FinanceOverviewResponse>> GetFinanceOverview(
        [FromQuery] DashboardPeriodRequest request,
        CancellationToken ct)
    {
        var result = await service.GetFinanceOverviewAsync(request, ct);
        return Ok(result);
    }


    [HttpGet("finance/cash-flow")]
    [SwaggerOperation(
        Summary = "Dòng tiền vào/ra và phân loại nguồn tiền",
        Description =
            "Trả External Inflow, External Outflow, Net Cash Flow, time series và breakdown. " +
            "GHN Shipping Collected chỉ là phần shipping thực sự được HomeCycle collect; " +
            "không bao gồm khoản HomeCycle thanh toán cho GHN.")]
    public async Task<ActionResult<FinanceCashFlowResponse>> GetFinanceCashFlow(
        [FromQuery] DashboardPeriodRequest request,
        CancellationToken ct)
    {
        var result = await service.GetFinanceCashFlowAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("finance/revenue")]
    [SwaggerOperation(
    Summary = "Doanh thu thực tế của nền tảng",
    Description =
        "Chỉ tính các WalletTransaction Completed thuộc Commission_Fee hoặc Subscription_Fee " +
        "đã chảy vào Platform_Revenue wallet. " +
        "Không tính GHN shipping escrow, payout release, refund hoặc toàn bộ số dư System Wallet.")]
    public async Task<ActionResult<FinanceRevenueResponse>> GetFinanceRevenue(
    [FromQuery] DashboardPeriodRequest request,
    CancellationToken ct)
    {
        var result = await service.GetFinanceRevenueAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("finance/payment-status")]
    [SwaggerOperation(
        Summary = "Tổng quan trạng thái và giá trị Payment",
        Description =
            "Phân loại các Payment được tạo trong kỳ theo trạng thái hiện tại. " +
            "Trả cả Count, Amount, Paid Rate, Failure Rate và Refunded Payment Rate.")]
    public async Task<ActionResult<FinancePaymentStatusResponse>> GetFinancePaymentStatus(
        [FromQuery] DashboardPeriodRequest request,
        CancellationToken ct)
    {
        var result = await service.GetFinancePaymentStatusAsync(request, ct);
        return Ok(result);
    }


    [HttpGet("finance/transactions")]
    [SwaggerOperation(
        Summary = "Sổ giao dịch tài chính gần đây",
        Description =
            "Phân trang WalletTransaction theo thời gian, loại giao dịch, trạng thái, reference và FinanceFlowScope. " +
            "Một WalletTransaction là một financial event; không trả từng WalletLedger row thành giao dịch riêng.")]
    public async Task<ActionResult<PagedResult<FinanceTransactionItem>>> GetFinanceTransactions(
        [FromQuery] FinanceTransactionRequest request,
        CancellationToken ct)
    {
        var result = await service.GetFinanceTransactionsAsync(request, ct);
        return Ok(result);
    }


    [HttpGet("finance/health")]
    [SwaggerOperation(
        Summary = "Theo dõi sức khỏe vận hành tài chính",
        Description =
            "Phát hiện payment Pending quá hạn, Pending thiếu expiry, withdrawal đang chờ/xử lý, " +
            "order quá hạn release vẫn còn Hold, order Completed thiếu dispute deadline, " +
            "tiền Hold do active dispute, wallet âm và financial transaction chưa phân loại.")]
    public async Task<ActionResult<FinanceHealthResponse>> GetFinanceHealth(
        [FromQuery] DashboardPeriodRequest request,
        CancellationToken ct)
    {
        var result = await service.GetFinanceHealthAsync(request, ct);
        return Ok(result);
    }
}
