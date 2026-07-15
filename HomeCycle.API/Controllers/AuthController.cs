using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.Interfaces.Services.Auths;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpPost("/Personal/Login")]
        public async Task<IActionResult> LoginPersonal([FromBody] LoginPersonalRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginPersonalAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("/Personal/Register")]
        public async Task<IActionResult> RegisterPersonal(
            [FromHeader(Name = "X-Registration-Token")] string registrationToken, // Lấy token từ Header
            [FromBody] RegisterPersonalRequest request,
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

        [HttpPost("/Personal/refresh-token")]
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
            await _authService.SendOtpAsync(request.Email);

            return Ok(new
            {
                Message = "OTP are sent!!!"
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
                    Message = result.Error?.Message ?? "OTP không đúng hoặc đã hết hạn."
                });
            }

            return Ok(new VerifyOtpResponse
            {
                Success = true,
                Message = "Xác thực email thành công.",
                RegistrationToken = result.Data
            });
        }

        [HttpPost("/business/register")]
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

    }
}
