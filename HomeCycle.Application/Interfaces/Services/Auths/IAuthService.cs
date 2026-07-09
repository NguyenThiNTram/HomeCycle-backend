using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Auths
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterPersonalAsync(RegisterPersonalRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponseDto>> LoginPersonalAsync(LoginPersonalRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    }
}
