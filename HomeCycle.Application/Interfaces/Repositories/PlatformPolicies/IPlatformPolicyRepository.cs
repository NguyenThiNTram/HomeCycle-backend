using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.PlatformPolicies
{
    public interface IPlatformPolicyRepository
    {
        Task<platform_policy?> GetActiveAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default);

        Task<platform_policy?> GetActiveForUpdateAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default);

        Task<int> GetNextVersionAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default);

        Task AddAsync(platform_policy policy, CancellationToken cancellationToken = default);

        Task UpdateAsync(platform_policy policy, CancellationToken cancellationToken = default);
    }
}
