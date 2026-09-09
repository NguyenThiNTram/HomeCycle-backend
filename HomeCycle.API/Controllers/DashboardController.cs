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
    [SwaggerOperation(Summary = "Tổng quan vận hành và tăng giảm so với kỳ trước",
        Description = "Giao dịch = Payment. Tổng toàn thời gian; xu hướng theo CreatedAt. From/To là ngày UTC+7, To không tính; mặc định 30 ngày hoàn tất. GroupBy: Day/Week/Month.")]
    public async Task<ActionResult<OperationOverviewResponse>> GetOperationOverview(
        [FromQuery] DashboardPeriodRequest request, CancellationToken ct)
        => Ok(await service.GetOperationOverviewAsync(request, ct));

    [HttpGet("payments")]
    [SwaggerOperation(Summary = "Thống kê thanh toán và xu hướng tạo mới",
        Description = "Các filter trạng thái/phương thức/loại áp dụng cho cả hai kỳ và các chỉ số sự kiện. PaidInPeriodCount gồm Completed/Refunded/PartiallyRefunded có PaidAt trong kỳ.")]
    public async Task<ActionResult<PaymentDashboardResponse>> GetPayments(
        [FromQuery] PaymentDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetPaymentDashboardAsync(request, ct));

    [HttpGet("orders")]
    [SwaggerOperation(Summary = "Thống kê đơn hàng và xu hướng tạo mới",
        Description = "Phân bố trạng thái của đơn tạo trong kỳ; các số hoàn thành/hủy/hoàn trả dùng timestamp tương ứng, cùng filter trạng thái hiện tại.")]
    public async Task<ActionResult<OrderDashboardResponse>> GetOrders(
        [FromQuery] OrderDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetOrderDashboardAsync(request, ct));

    [HttpGet("appointments")]
    [SwaggerOperation(Summary = "Thống kê lịch hẹn và xu hướng tạo mới",
        Description = "Đếm bản ghi, bao gồm đề xuất đổi lịch. ScheduledDateInPeriodCount là lịch có ngày hẹn trong kỳ, không mặc định loại lịch Cancelled/Expired; dùng filter nếu cần.")]
    public async Task<ActionResult<AppointmentDashboardResponse>> GetAppointments(
        [FromQuery] AppointmentDashboardRequest request, CancellationToken ct)
        => Ok(await service.GetAppointmentDashboardAsync(request, ct));

    [HttpGet("disputes")]
    [SwaggerOperation(Summary = "Phân bố case tranh chấp, nhiều nhất và ít nhất",
        Description = "Tỷ lệ trên tranh chấp tạo trong kỳ sau filter. Ít nhất chỉ xét case có lượt chọn, hỗ trợ đồng hạng. Unspecified là dữ liệu category không xác định.")]
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
