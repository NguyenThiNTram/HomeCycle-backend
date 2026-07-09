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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
    }
}
