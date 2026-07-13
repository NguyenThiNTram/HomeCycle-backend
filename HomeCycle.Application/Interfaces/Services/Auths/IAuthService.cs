using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
using HomeCycle.Application.DTOs.Responses.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Auths
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterPersonalAsync(string email, RegisterPersonalRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponseDto>> LoginPersonalAsync(LoginPersonalRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        //login = gg
        Task<Result<GoogleAuthResponseDto>> ExecuteGoogleLoginAsync(string idToken, CancellationToken cancellationToken = default);

        //otp
        Task SendOtpAsync(string email);
        Task<Result<string>> VerifyOtpAsync(string email, string code);
    }
}
