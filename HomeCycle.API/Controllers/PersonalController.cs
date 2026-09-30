using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Users;
using HomeCycle.Application.Interfaces.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/personal-profiles")]
    [ApiController]
    [Authorize(Roles = "Personal")]
    public class PersonalController : ControllerBase
    {
        private readonly IPersonalProfileService _personalProfileService;

        public PersonalController(IPersonalProfileService personalProfileService)
        {
            _personalProfileService = personalProfileService;
        }

        // Lấy thông tin profile
        [HttpGet("me")]
        [SwaggerOperation(
            Summary = "Lấy thông tin cá nhân",
            Description = "Trả về thông tin chi tiết hồ sơ cá nhân của người dùng hiện tại."
        )]
        //[ProducesResponseType(typeof(Result<PersonalProfileResponse>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.GetMyProfileAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        // update thông tin
        [HttpPatch("me/profile")]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin cá nhân",
            Description = "Cập nhật thông tin hồ sơ cá nhân của người dùng hiện tại."
        )]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePersonalProfileRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateProfileAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // update avt
        [HttpPatch("me/avatar")]
        [SwaggerOperation(
            Summary = "Cập nhật ảnh đại diện",
            Description = "Cập nhật ảnh đại diện (avatar) cho người dùng hiện tại."
        )]
        [Consumes("multipart/form-data")]
        //[ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAvatar([FromForm] UpdateAvatarRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateAvatarAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // update giấy tờ
        [HttpPatch("me/identity")]
        [SwaggerOperation(
            Summary = "Cập nhật giấy tờ tùy thân",
            Description = "Cập nhật thông tin giấy tờ tùy thân (CMND/CCCD) của người dùng hiện tại."
        )]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateIdentity([FromForm] UpdateIdCardRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateIdentityAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("me/identity/scan")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(IdentityDocumentScanRequest.MaxMultipartBodyBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = IdentityDocumentScanRequest.MaxMultipartBodyBytes)]
        public async Task<IActionResult> ScanIdentity(
            [FromForm] IdentityDocumentScanRequest request,
            CancellationToken cancellationToken)
        {
            Response.Headers["Cache-Control"] = "no-store";
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.ScanIdentityAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return IdentityScanError(result.Error!.Code, result.Error.Message);

            return Ok(new { success = true, data = result.Data });
        }

        private IActionResult IdentityScanError(string code, string message)
        {
            var payload = new { success = false, code, message, traceId = HttpContext.TraceIdentifier };
            return code switch
            {
                "IDENTITY_SCAN_ROLE_MISMATCH" => StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "IDENTITY_SCAN_ROLE_FORBIDDEN", message = "Tài khoản không có quyền quét CCCD theo luồng này.", traceId = HttpContext.TraceIdentifier }),
                "IDENTITY_SCAN_RATE_LIMITED" => StatusCode(StatusCodes.Status429TooManyRequests, payload),
                "IDENTITY_SCAN_TEMPORARILY_UNAVAILABLE" => StatusCode(StatusCodes.Status503ServiceUnavailable, payload),
                "IDENTITY_SCAN_TIMEOUT" => StatusCode(StatusCodes.Status504GatewayTimeout, payload),
                "IDENTITY_SCAN_PROVIDER_CONFIGURATION_ERROR" or "IDENTITY_SCAN_PROVIDER_INVALID_RESPONSE" => StatusCode(StatusCodes.Status502BadGateway, payload),
                _ => BadRequest(payload)
            };
        }

        // update bank
        [HttpPatch("me/bank")]
        [SwaggerOperation(
            Summary = "Cập nhật thông tin ngân hàng",
            Description = "Cập nhật thông tin tài khoản ngân hàng của người dùng hiện tại."
        )]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBank([FromBody] UpdateBankAccountRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateBankAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // Trích UserId từ Token để tránh truyền ID qua API (Bảo mật IDOR) -- test
        // <returns>Guid của user đang thực hiện request</returns>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Cannot authenticate user identity from Token.");
            }

            return userId;
        }
    }
}
