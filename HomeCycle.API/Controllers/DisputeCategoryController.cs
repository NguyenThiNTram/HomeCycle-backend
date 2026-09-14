using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/dispute-categories")]
    [Authorize]
    public class DisputeCategoryController : ControllerBase
    {
        private readonly IDisputeCategoryService _service;

        public DisputeCategoryController(IDisputeCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get active dispute categories",
            Description = "Returns active dispute categories available for creating disputes. The result can optionally be filtered by dispute target type."
        )]
        public async Task<IActionResult> GetActive(
            [FromQuery] DisputeTargetType? targetType,
            CancellationToken ct)
        {
            var result = await _service.GetActiveOptionsAsync(targetType, ct);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("~/api/admin/dispute-categories")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [SwaggerOperation(
            Summary = "Get dispute categories for administration",
            Description = "Returns dispute categories for Admin management. The result can optionally be filtered by active or inactive status."
        )]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isActive,
            CancellationToken ct)
        {
            var result = await _service.GetAllAsync(isActive, ct);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("~/api/admin/dispute-categories")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [SwaggerOperation(
            Summary = "Create a dispute category",
            Description = "Creates a new dispute category and assigns the dispute target types where the category can be used."
        )]
        public async Task<IActionResult> Create(
            [FromBody] CreateDisputeCategoryRequest request,
            CancellationToken ct)
        {
            var result = await _service.CreateAsync(
                CurrentUserId,
                request,
                ct);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPatch("~/api/admin/dispute-categories/{categoryId:int}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [SwaggerOperation(
            Summary = "Update a dispute category",
            Description = "Updates the name, description, or supported target types of an existing dispute category. The category code remains unchanged."
        )]
        public async Task<IActionResult> Update(
            int categoryId,
            [FromBody] UpdateDisputeCategoryRequest request,
            CancellationToken ct)
        {
            var result = await _service.UpdateAsync(
                categoryId,
                request,
                ct);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPatch("~/api/admin/dispute-categories/{categoryId:int}/status")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [SwaggerOperation(
            Summary = "Enable or disable a dispute category",
            Description = "Changes the active status of a dispute category. Disabled categories remain available for historical disputes but cannot be used to create new disputes."
        )]
        public async Task<IActionResult> UpdateStatus(
            int categoryId,
            [FromBody] UpdateDisputeCategoryStatusRequest request,
            CancellationToken ct)
        {
            var result = await _service.UpdateStatusAsync(
                categoryId,
                request.IsActive,
                ct);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        private Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(claim) ||
                    !Guid.TryParse(claim, out var userId))
                {
                    throw new UnauthorizedAccessException(
                        "Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");
                }

                return userId;
            }
        }
    }
}
