using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IEmailService emailService, IUserService userService)
        {
            _authService = authService;
            _emailService = emailService;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login( [FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("personal/register")]
        public async Task<IActionResult> RegisterPersonal(
            [FromHeader(Name = "X-Registration-Token")] string registrationToken, // Lấy token từ Header
            [FromForm] RegisterPersonalRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(registrationToken))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Vui lòng cung cấp mã xác thực phiên đăng ký (X-Registration-Token)."
                });
            }

            var result = await _authService.RegisterPersonalAsync(registrationToken, request, cancellationToken);

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
                message = "Register personal successful.",
                data = result.Data
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var result = await _authService.ExecuteGoogleLoginAsync(request.IdToken);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] EmailDto request)
        {
            var result = await _authService.SendOtpAsync(request.Email);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new
            {
                Message = result.Data
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request.Email, request.Otp);

            if (!result.IsSuccess)
            {
                return BadRequest(new VerifyOtpResponse
                {
                    Success = false,
                    Message = result.Error?.Message ?? "Invalid or expired OTP"
                });
            }

            return Ok(new VerifyOtpResponse
            {
                Success = true,
                Message = "Email verified successfully!",
                RegistrationToken = result.Data
            });
        }

        [HttpPost("business/register")]
        public async Task<IActionResult> RegisterBusinessAccount(
            [FromHeader(Name = "X-Registration-Token")] string registrationToken,
            [FromBody] RegisterBusinessAccountRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(registrationToken))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Registration session verification failed. Please provide 'X-Registration-Token' header."
                });
            }

            var result = await _authService.RegisterBusinessAccountAsync(registrationToken, request, cancellationToken);

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
                message = result.Data.Message,
                data = result.Data
            });
        }

        // Lấy danh sách người dùng (lọc theo role, status, keyword) — chỉ dành cho Admin
        [HttpGet("admin/users")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] GetAllUsersRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.GetAllUsersAsync(request, cancellationToken);

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

        // Khoá tài khoản (chuyển Status -> Suspended) — chỉ dành cho Admin
        [HttpPost("admin/users/{userId:guid}/lock")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> LockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _authService.LockUserAsync(adminId, userId, cancellationToken);

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
                message = "Tài khoản đã bị khoá.",
                data = result.Data
            });
        }

        // Mở khoá tài khoản (chuyển Status -> Active) — chỉ dành cho Admin
        [HttpPost("admin/users/{userId:guid}/unlock")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _authService.UnlockUserAsync(adminId, userId, cancellationToken);

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
                message = "Tài khoản đã được mở khoá.",
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
