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
public sealed class DashboardController(IUserDashboardService service) : ControllerBase
{
    [HttpGet("users/overview")]
    [SwaggerOperation(Summary = "Tổng quan tài khoản và số lượng theo trạng thái/role",
        Description = "Mặc định bao gồm mọi role và trạng thái Deleted. Role là bộ lọc tùy chọn. Active là trạng thái tài khoản, không phải đang online.")]
    public async Task<ActionResult<UserDashboardOverviewResponse>> GetUserOverview(
        [FromQuery] UserDashboardScopeRequest request, CancellationToken ct)
        => Ok(await service.GetOverviewAsync(request.Role, ct));

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
        => Ok(await service.GetRegistrationTrendAsync(request, ct));
}
