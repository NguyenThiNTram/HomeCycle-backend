using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.Interfaces.Services.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/business-profiles")]
    [ApiController]
    [Authorize]
    public class BusinessProfileController : ControllerBase
    {
        private readonly IBusinessProfileService _businessProfileService;

        public BusinessProfileController(IBusinessProfileService businessProfileService)
        {
            _businessProfileService = businessProfileService;
        }

        [HttpGet("registration-detail")]
        public async Task<IActionResult> GetRegistrationDetail(CancellationToken cancellationToken)
        {

            var userId = GetCurrentUserId();
            var result = await _businessProfileService.GetRegistrationDetailAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Error.Message });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitBusinessProfile(
            [FromForm] SubmitBusinessProfileRequest request,
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

            return result.IsSuccess ? Ok(new { success = true, data = result.Data }) : BadRequest(result.Error);
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

        [HttpGet]
        public async Task<IActionResult> GetBusinessProfile(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.GetBusinessProfileAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //[HttpPut("documents")]
        //public async Task<IActionResult> UpdateDocuments(
        //    [FromForm] UpdateBusinessDocumentsRequest request,
        //    CancellationToken cancellationToken)
        //{
        //    var userId = GetCurrentUserId();
        //    var result = await _businessProfileService.UpdateBusinessDocumentsAsync(userId, request, cancellationToken);

        //    if (!result.IsSuccess)
        //        return BadRequest(result);

        //    return Ok(result);
        //}


        [HttpPut("username")]
        public async Task<IActionResult> UpdateUsername(
            [FromBody] UpdateUsernameRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateUsernameAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("phone-number")]
        public async Task<IActionResult> UpdatePhoneNumber(
            [FromBody] UpdatePhoneNumberRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdatePhoneNumberAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(
            [FromForm] UpdateAvatarRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateAvatarAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("bank-account")]
        public async Task<IActionResult> UpdateBankAccount(
            [FromBody] UpdateBankAccountRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateBankAccountAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        //[HttpPut("service-areas")]
        //public async Task<IActionResult> UpdateServiceAreas(
        //    [FromBody] UpdateBusinessServiceAreasRequest request,
        //    CancellationToken cancellationToken)
        //{
        //    var userId = GetCurrentUserId();
        //    var result = await _businessProfileService.UpdateBusinessServiceAreasAsync(userId, request, cancellationToken);

        //    if (!result.IsSuccess)
        //        return BadRequest(result);

        //    return Ok(result);
        //}

        [HttpPut("identity")]
        public async Task<IActionResult> UpdateIdentity(
            [FromForm] UpdateIdentityRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateIdentityAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("business-registration")]
        public async Task<IActionResult> UpdateBusinessRegistration(
            [FromForm] UpdateBusinessRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateBusinessRegistrationAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("service-areas")]
        public async Task<IActionResult> CreateServiceArea(
            [FromBody] BusinessServiceAreaRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.CreateBusinessServiceAreaAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            return Ok(new { success = true, data = new { businessServiceAreaId = result.Data } });
        }

        [HttpPut("service-areas/{businessServiceAreaId:guid}")]
        public async Task<IActionResult> UpdateServiceArea(
            [FromRoute] Guid businessServiceAreaId,
            [FromBody] BusinessServiceAreaRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.UpdateBusinessServiceAreaAsync(userId, businessServiceAreaId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            return Ok(new { success = true });
        }

        [HttpDelete("service-areas/{businessServiceAreaId:guid}")]
        public async Task<IActionResult> DeleteServiceArea(
            [FromRoute] Guid businessServiceAreaId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _businessProfileService.DeleteBusinessServiceAreaAsync(userId, businessServiceAreaId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            return Ok(new { success = true });
        }
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

            return userId;
        }
    }
}
