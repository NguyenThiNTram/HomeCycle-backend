using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Security
{
    public interface IOtpRepository
    {
        Task AddModeratorTokenAsync(otp token, CancellationToken cancellationToken = default);
        Task<otp?> GetModeratorTokenAsync(string hash, string purpose, CancellationToken cancellationToken = default);
        Task<bool> ConsumeModeratorTokenAsync(Guid tokenId, string purpose, CancellationToken cancellationToken = default);
        Task<bool> UpdateModeratorActivationAsync(Guid userId, string? passwordHash, CancellationToken cancellationToken = default);
        Task AddAsync(otp otp);

        Task<otp?> GetValidOtpAsync(string email, string code);

        Task UpdateAsync(otp otp);

        Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken = default);

        Task UpdateUserIdAsync(string email, Guid userId, CancellationToken cancellationToken);
    }
}
