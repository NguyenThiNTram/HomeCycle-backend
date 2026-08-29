using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using HomeCycle.Application.DTOs.Responses.PlatformPolicies;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.PlatformPolicies
{
    public interface IPlatformPolicyService
    {
        Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> GetDisputePolicyAsync(CancellationToken cancellationToken = default);

        Task<Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>> GetAppointmentPolicyAsync(CancellationToken cancellationToken = default);

        Task<Result<PlatformPolicyResponseDto<DisputePolicyConfigDto>>> UpdateDisputePolicyAsync(Guid adminId, UpdateDisputePolicyRequest request, CancellationToken cancellationToken = default);

        Task<Result<PlatformPolicyResponseDto<AppointmentPolicyConfigDto>>> UpdateAppointmentPolicyAsync(Guid adminId, UpdateAppointmentPolicyRequest request, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<PlatformPolicySummaryResponseDto>>> GetAllActiveAsync(CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<PlatformPolicyVersionListItemDto>>> GetVersionsAsync(PlatformPolicyType policyType, CancellationToken cancellationToken = default);

        Task<Result<PlatformPolicyVersionDetailDto>> GetVersionAsync(PlatformPolicyType policyType, int version, CancellationToken cancellationToken = default);

        Task<Result<PlatformPolicyVersionDetailDto>> RestoreVersionAsync(Guid adminId, PlatformPolicyType policyType, int version, CancellationToken cancellationToken = default);
    }
}
