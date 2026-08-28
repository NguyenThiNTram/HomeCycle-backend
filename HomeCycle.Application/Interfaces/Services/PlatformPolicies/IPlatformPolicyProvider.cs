using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.PlatformPolicies
{
    public interface IPlatformPolicyProvider
    {
        Task<DisputePolicyConfigDto> GetDisputeConfigAsync(CancellationToken cancellationToken = default);

        Task<AppointmentPolicyConfigDto> GetAppointmentConfigAsync(CancellationToken cancellationToken = default);
    }
}
