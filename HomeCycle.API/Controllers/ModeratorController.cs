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

        [HttpPost("business-profiles/review")]
        public async Task<IActionResult> ReviewBusinessProfile(
            [FromBody] ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _moderatorService.ReviewBusinessProfileAsync(moderatorId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Data
            });
        }

        [HttpGet("business-profiles/{id}")]
        public async Task<IActionResult> GetBusinessProfileDetail(
            [FromRoute] Guid profileId,
            CancellationToken cancellationToken)
        {
            var result = await _moderatorService.GetBusinessProfileDetailForModeratorAsync(profileId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpGet("business-profiles/pending")]
        public async Task<IActionResult> GetPendingBusinessProfiles(
            [FromQuery] string? keyword,
            CancellationToken cancellationToken)
        {
            var result = await _moderatorService.GetPendingBusinessProfilesAsync(keyword, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
