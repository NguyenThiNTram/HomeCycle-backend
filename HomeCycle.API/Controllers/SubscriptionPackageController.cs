using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Services.SubscriptionPackages;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/admin/subscription-packages")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class SubscriptionPackageController : ControllerBase
    {
        private readonly ISubscriptionPackageService _service;

        public SubscriptionPackageController(ISubscriptionPackageService service)
        {
            _service = service;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách gói đăng ký",
            Description = "Trả về các gói đăng ký để Admin quản lý, hỗ trợ lọc theo trạng thái và role áp dụng.")]
        [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPackageResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isActive,
            [FromQuery] UserRole? targetRole,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetAllAsync(
                isActive,
                targetRole,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpGet("{packageId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết gói đăng ký",
            Description = "Trả về thông tin gói đăng ký và toàn bộ entitlement được cấu hình cho gói.")]
        [ProducesResponseType(typeof(SubscriptionPackageResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(
            Guid packageId,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(
                packageId,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpGet("entitlement-definitions")]
        [SwaggerOperation(
            Summary = "Lấy danh mục entitlement được hỗ trợ",
            Description = "Trả về các entitlement backend hỗ trợ để Admin cấu hình đúng key, kiểu dữ liệu, role và khả năng unlimited.")]
        [ProducesResponseType(typeof(IReadOnlyList<EntitlementDefinitionResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetEntitlementDefinitions()
        {
            var result = _service.GetEntitlementDefinitions();
            return Ok(result.Data);
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo gói đăng ký",
            Description = "Tạo gói đăng ký mới cùng các entitlement. Code và TargetRole không thể thay đổi sau khi tạo.")]
        [ProducesResponseType(typeof(SubscriptionPackageResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSubscriptionPackageRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty)
                return Unauthorized();

            var result = await _service.CreateAsync(
                adminId,
                request,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpPatch("{packageId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật gói đăng ký",
            Description = "Cập nhật tên, mô tả, giá, thời hạn hoặc toàn bộ entitlement của gói. Code và TargetRole được giữ nguyên.")]
        [ProducesResponseType(typeof(SubscriptionPackageResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            Guid packageId,
            [FromBody] UpdateSubscriptionPackageRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty)
                return Unauthorized();

            var result = await _service.UpdateAsync(
                adminId,
                packageId,
                request,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpPatch("{packageId:guid}/status")]
        [SwaggerOperation(
            Summary = "Bật hoặc tắt gói đăng ký",
            Description = "Thay đổi trạng thái hoạt động của gói. Gói bị tắt vẫn được giữ lại để phục vụ dữ liệu lịch sử.")]
        [ProducesResponseType(typeof(SubscriptionPackageResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(
            Guid packageId,
            [FromBody] UpdateSubscriptionPackageStatusRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty)
                return Unauthorized();

            var result = await _service.UpdateStatusAsync(
                adminId,
                packageId,
                request.IsActive,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        private IActionResult MapError(Error? error)
        {
            return error?.Code switch
            {
                "VALIDATION_ERROR" or "SubscriptionPackage.InvalidEntitlement"
                    => BadRequest(error),
                "SubscriptionPackage.NotFound"
                    => NotFound(error),
                "SubscriptionPackage.CodeAlreadyExists" or
                "SubscriptionPackage.NameAlreadyExists"
                    => Conflict(error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, error)
            };
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
        }
    }
}
