using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Services.Dashboard;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
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
        Description = "Tổng, active, phân bố trạng thái và tuổi active là snapshot hiện tại. Chuỗi kết quả dùng CompletedAt, CancelledAt và ReturnedAt trong kỳ. Active gồm Pending, Processing và Disputing.")]
    public async Task<ActionResult<OrderDashboardResponse>> GetOrders(
        [FromQuery] OrderDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetOrderDashboardAsync(request, ct));

    [HttpGet("appointments")]
    [SwaggerOperation(Summary = "Thống kê lịch hẹn theo lịch thực tế và tồn đọng",
        Description = "Tổng, upcoming, hôm nay, pending, expired, overdue và phân bố là snapshot hiện tại. Chuỗi lịch dùng InspectionDate hoặc CollectionDate; hoàn tất/hủy dùng timestamp nghiệp vụ tương ứng. Ngày tính theo UTC+7.")]
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
        Description = "Theo role Business hiện tại, bao gồm Deleted nếu không lọc. Lọc model/profileStatus loại tài khoản chưa có hồ sơ. Survey coverage dùng số có hồ sơ làm mẫu số.")]
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
}
