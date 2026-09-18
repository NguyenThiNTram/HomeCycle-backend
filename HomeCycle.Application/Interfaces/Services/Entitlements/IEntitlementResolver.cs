using HomeCycle.Application.Entitlements;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Entitlements
{
    public interface IEntitlementResolver
    {
        Task<int> ResolveAiDailyLimitAsync(Guid userId, UserRole role, DateTime atUtc, CancellationToken cancellationToken = default);
        Task<EffectiveWithdrawalEntitlements> ResolveWithdrawalAsync(
            Guid userId,
            decimal baselineDailyAmountLimit,
            int baselineDailyCountLimit,
            DateTime atUtc,
            CancellationToken cancellationToken = default);
    }
}
