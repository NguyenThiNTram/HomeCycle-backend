using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public class DisputeWindowPolicy
       : IDisputeWindowPolicy
    {
        private const int NormalWindowHours = 72;
        private const int LowReputationWindowHours = 120;
        private const int LowReputationThreshold = 60;

        private readonly IUserRepository _userRepository;
        private readonly IPersonalProfileRepository
            _personalProfileRepository;
        private readonly IBusinessProfileRepository
            _businessProfileRepository;

        public DisputeWindowPolicy(
            IUserRepository userRepository,
            IPersonalProfileRepository personalProfileRepository,
            IBusinessProfileRepository businessProfileRepository)
        {
            _userRepository = userRepository;
            _personalProfileRepository =
                personalProfileRepository;
            _businessProfileRepository =
                businessProfileRepository;
        }

        public async Task<TimeSpan>
            GetOrderDisputeWindowAsync(
                Guid sellerId,
                CancellationToken cancellationToken = default)
        {
            var seller =
                await _userRepository.GetByIdAsync(
                    sellerId,
                    cancellationToken);

            if (seller == null)
            {
                return TimeSpan.FromHours(
                    NormalWindowHours);
            }

            int? reputationScore = null;

            if (seller.Role == UserRole.Personal)
            {
                var profile =
                    await _personalProfileRepository
                        .GetByUserIdAsync(
                            sellerId,
                            cancellationToken);

                reputationScore =
                    profile?.ReputationScore;
            }
            else if (seller.Role == UserRole.Business)
            {
                var profile =
                    await _businessProfileRepository
                        .GetByUserIdAsync(
                            sellerId,
                            cancellationToken);

                reputationScore =
                    profile?.ReputationScore;
            }

            if (reputationScore.HasValue &&
                reputationScore.Value
                    < LowReputationThreshold)
            {
                return TimeSpan.FromHours(
                    LowReputationWindowHours);
            }

            return TimeSpan.FromHours(
                NormalWindowHours);
        }
    }
}
