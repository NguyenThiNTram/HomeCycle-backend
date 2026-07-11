using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories
{
    public interface IOtpRepository
    {
        Task AddAsync(otp otp);

        Task<otp?> GetValidOtpAsync(string email, string code);

        Task UpdateAsync(otp otp);

        Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken = default);
    }
}
