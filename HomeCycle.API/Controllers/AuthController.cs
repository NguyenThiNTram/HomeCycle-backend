using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.Interfaces.Services.Auths;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            [FromBody] RegisterPersonalRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _authService
                .RegisterPersonalAsync(request, cancellationToken);

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
                Message = "OTP are sended!!!"
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request.Email, request.Otp);

            if (!result)
            {
                return BadRequest(new
                {
                    Message = "OTP không đúng hoặc đã hết hạn."
                });
            }
            return Ok(new
            {
                Success = true,
                Message ="Xác thực email thành công."
            });
        }
    }
}
