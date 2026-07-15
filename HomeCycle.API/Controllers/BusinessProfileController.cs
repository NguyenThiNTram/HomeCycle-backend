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

   
        [HttpGet("status")]
        [Authorize] // Ép buộc gửi kèm JWT
        public async Task<IActionResult> GetProfileStatus(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { success = false, message = "Không tìm thấy định danh tài khoản trong phiên làm việc." });

            var userId = Guid.Parse(userIdClaim);

            // Nhận trực tiếp Result<BusinessProfileStatus?>
            var result = await _businessProfileService.GetProfileStatusAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            // Trả thẳng giá trị Enum hoặc null về cho Frontend
            return Ok(new { success = true, data = result.Data });
        }

        //[HttpGet("registration-detail")]
        //[Authorize]
        //public async Task<IActionResult> GetRegistrationDetail(CancellationToken cancellationToken)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userIdClaim))
        //        return Unauthorized(new { success = false, message = "Không tìm thấy thông tin tài khoản trong phiên làm việc." });

        //    var userId = Guid.Parse(userIdClaim);
        //    var result = await _businessProfileService.GetRegistrationDetailAsync(userId, cancellationToken);

        //    if (!result.IsSuccess)
        //        return BadRequest(new { success = false, message = result.Error.Message });

        //    return Ok(new { success = true, data = result.Data });
        //}

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitBusinessProfile(
            [FromBody] SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { success = false, message = "Không tìm thấy định danh tài khoản trong phiên làm việc." });

            var userId = Guid.Parse(userIdClaim);
            var result = await _businessProfileService.SubmitBusinessProfileAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, code = result.Error.Code, message = result.Error.Message });

            return Ok(new { success = true, message = result.Data });
        }


    }
}
