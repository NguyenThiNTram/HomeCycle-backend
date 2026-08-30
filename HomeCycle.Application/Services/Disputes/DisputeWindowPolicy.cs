using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class DisputeWindowPolicy : IDisputeWindowPolicy
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;

        public DisputeWindowPolicy(
            IUserRepository userRepository,
            IPersonalProfileRepository personalProfileRepository,
            IBusinessProfileRepository businessProfileRepository,
            IPlatformPolicyProvider platformPolicyProvider)
        {
            _userRepository = userRepository;
            _personalProfileRepository = personalProfileRepository;
            _businessProfileRepository = businessProfileRepository;
            _platformPolicyProvider = platformPolicyProvider;
        }

        public async Task<TimeSpan> GetOrderDisputeWindowAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
        {
            var config = await _platformPolicyProvider.GetDisputeConfigAsync(cancellationToken);
            var seller = await _userRepository.GetByIdAsync(sellerId, cancellationToken);

            if (seller == null)
                return TimeSpan.FromDays(config.NormalDisputeWindowDays);

            int? reputationScore = null;

            if (seller.Role == UserRole.Personal)
            {
                var profile = await _personalProfileRepository.GetByUserIdAsync(sellerId, cancellationToken);
                reputationScore = profile?.ReputationScore;
            }
            else if (seller.Role == UserRole.Business)
            {
                var profile = await _businessProfileRepository.GetByUserIdAsync(sellerId, cancellationToken);
                reputationScore = profile?.ReputationScore;
            }

            if (reputationScore.HasValue && reputationScore.Value < config.LowReputationThreshold)
                return TimeSpan.FromDays(config.LowReputationDisputeWindowDays);

            return TimeSpan.FromDays(config.NormalDisputeWindowDays);
        }
    }
}
