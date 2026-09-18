using HomeCycle.Application.Commons.Errors;
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
    [Route("api/subscription-packages")]
    public class SubscriptionPackageController : ControllerBase
    {
        private readonly ISubscriptionPackageService _service;
        private readonly IUserSubscriptionService _userSubscriptionService;

        public SubscriptionPackageController(
            ISubscriptionPackageService service,
            IUserSubscriptionService userSubscriptionService)
        {
            _service = service;
            _userSubscriptionService = userSubscriptionService;
        }

        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Lấy các gói đăng ký đang hoạt động",
            Description = "Trả về các gói đăng ký đang hoạt động và có thể được người dùng mua.")]
        [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPackageResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicPackages(CancellationToken cancellationToken)
        {
            var result = await _service.GetAllAsync(
                true,
                null,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpGet("{packageId:guid}")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Lấy chi tiết gói đăng ký",
            Description = "Trả về thông tin và entitlement của một gói đăng ký đang hoạt động.")]
        [ProducesResponseType(typeof(SubscriptionPackageResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicPackageById(
            Guid packageId,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(
                packageId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            if (result.Data == null || !result.Data.IsActive)
                return NotFound(SubscriptionPackageErrors.NotFound);

            return Ok(result.Data);
        }

        [HttpGet("me/subscription")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Lấy subscription hiện tại của người dùng",
            Description = "Trả về subscription Pending hoặc Active còn hiệu lực. Nếu không có subscription hiện tại thì trả null.")]
        [ProducesResponseType(typeof(UserSubscriptionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySubscription(
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _userSubscriptionService.GetCurrentAsync(
                userId,
                DateTime.UtcNow,
                cancellationToken);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapError(result.Error);
        }

        [HttpGet("~/api/admin/subscription-packages")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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

        [HttpGet("~/api/admin/subscription-packages/{packageId:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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

        [HttpGet("~/api/admin/subscription-packages/entitlement-definitions")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [SwaggerOperation(
            Summary = "Lấy danh mục entitlement được hỗ trợ",
            Description = "Trả về các entitlement backend hỗ trợ để Admin cấu hình đúng key, kiểu dữ liệu, role và khả năng unlimited.")]
        [ProducesResponseType(typeof(IReadOnlyList<EntitlementDefinitionResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetEntitlementDefinitions()
        {
            var result = _service.GetEntitlementDefinitions();
            return Ok(result.Data);
        }

        [HttpPost("~/api/admin/subscription-packages")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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

        [HttpPatch("~/api/admin/subscription-packages/{packageId:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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

        [HttpPatch("~/api/admin/subscription-packages/{packageId:guid}/status")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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
                "VALIDATION_ERROR" or
                "SubscriptionPackage.InvalidEntitlement"
                    => BadRequest(error),

                "SubscriptionPackage.NotFound" or
                "UserSubscription.NotFound"
                    => NotFound(error),

                "SubscriptionPackage.CodeAlreadyExists" or
                "SubscriptionPackage.NameAlreadyExists"
                    => Conflict(error),

                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    error)
            };
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(claim, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

}
