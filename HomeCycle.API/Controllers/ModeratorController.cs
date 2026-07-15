using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.Interfaces.Services.Moderators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/moderator")]
    [ApiController]
    [Authorize(Roles = "Moderator")]
    public class ModeratorController : ControllerBase
    {
        private readonly IModeratorService _moderatorService;

        public ModeratorController(IModeratorService moderatorService)
        {
            _moderatorService = moderatorService;
        }

        [HttpPost("business-profile/{userId}/review")]
        public async Task<IActionResult> ReviewBusinessProfile(
            [FromRoute] Guid userId,
            [FromBody] ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(moderatorIdClaim))
                return Unauthorized(new { success = false, message = "Phiên làm việc quản trị của bạn không hợp lệ." });

            var moderatorId = Guid.Parse(moderatorIdClaim);

            var result = await _moderatorService.ReviewBusinessProfileAsync(
                moderatorId,
                userId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Error.Message });

            return Ok(new { success = true, message = result.Data });
        }
    }
}
