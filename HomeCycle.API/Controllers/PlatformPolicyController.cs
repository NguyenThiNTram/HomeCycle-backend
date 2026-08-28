using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/admin/platform-policies")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PlatformPolicyController : ControllerBase
    {
        private readonly IPlatformPolicyService _platformPolicyService;

        public PlatformPolicyController(IPlatformPolicyService platformPolicyService)
        {
            _platformPolicyService = platformPolicyService;
        }

        [HttpGet("dispute")]
        public async Task<IActionResult> GetDisputePolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetDisputePolicyAsync(cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("dispute")]
        public async Task<IActionResult> UpdateDisputePolicy(
            [FromBody] UpdateDisputePolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty) return Unauthorized();

            var result = await _platformPolicyService.UpdateDisputePolicyAsync(adminId, request, cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("appointment")]
        public async Task<IActionResult> GetAppointmentPolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetAppointmentPolicyAsync(cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("appointment")]
        public async Task<IActionResult> UpdateAppointmentPolicy(
            [FromBody] UpdateAppointmentPolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty) return Unauthorized();

            var result = await _platformPolicyService.UpdateAppointmentPolicyAsync(adminId, request, cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
