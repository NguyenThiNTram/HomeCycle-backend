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
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login( [FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        //[HttpPost("/Personal/Login")]
        //public async Task<IActionResult> LoginPersonal([FromBody] LoginPersonalRequest request, CancellationToken cancellationToken)
        //{
        //    var result = await _authService.LoginPersonalAsync(request, cancellationToken);

        //    if (!result.IsSuccess)
        //        return BadRequest(result.Error);

        //    return Ok(result.Data);
        //}

        [HttpPost("Personal/Register")]
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
    }
}
