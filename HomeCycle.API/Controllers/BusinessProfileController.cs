using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.Interfaces.Services.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/business-profiles")]
    [ApiController]
    public class BusinessProfileController : ControllerBase
    {
        private readonly IBusinessProfileService _businessProfileService;

        public BusinessProfileController(IBusinessProfileService businessProfileService)
        {
            _businessProfileService = businessProfileService;
        }

        [HttpGet("registration-detail")]
        [Authorize]
        public async Task<IActionResult> GetRegistrationDetail(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin tài khoản trong phiên làm việc." });

            var userId = Guid.Parse(userIdClaim);
            var result = await _businessProfileService.GetRegistrationDetailAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Error.Message });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitBusinessProfile(
            [FromBody] SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.SubmitBusinessProfileAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            return Ok(new { success = true, message = result.Data });
        }

        [HttpGet("onboarding-status")]
        public async Task<IActionResult> GetOnboardingStatus(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.GetOnboardingStatusAsync(userId, cancellationToken);

            return result.IsSuccess ? Ok(new { success = true, data = (int)result.Data }) : BadRequest(result.Error);
        }

        [HttpPost("survey")]
        public async Task<IActionResult> SaveSurvey(
            [FromBody] SubmitBusinessSurveyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.SaveProcurementPreferenceAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Ok(new { success = true }) : BadRequest(result.Error);
        }

        [HttpGet("survey-detail")]
        public async Task<IActionResult> GetSurveyDetail(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.GetProcurementPreferenceAsync(userId, cancellationToken);

            return result.IsSuccess ? Ok(new { success = true, data = result.Data }) : BadRequest(result.Error);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}
