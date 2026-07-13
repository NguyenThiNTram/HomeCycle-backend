using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Users;
using HomeCycle.Application.Interfaces.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/personals")]
    [ApiController]
    //[Authorize(Roles = "Personal")]
    public class PersonalController : ControllerBase
    {
        private readonly IPersonalProfileService _personalProfileService;

        public PersonalController(IPersonalProfileService personalProfileService)
        {
            _personalProfileService = personalProfileService;
        }

        // Lấy thông tin profile
        [HttpGet("me")]
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
        [HttpPut("me/profile")]
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
        //[ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateAvatarAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // update giấy tờ
        [HttpPut("me/identity")]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateIdentity([FromBody] UpdateIdCardRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _personalProfileService.UpdateIdentityAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // update bank
        [HttpPut("me/bank")]
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
                throw new UnauthorizedAccessException("Không thể xác thực danh tính người dùng từ Token.");
            }

            return userId;
        }
    }
}
