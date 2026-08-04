using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.DTOs.Responses.Moderators;
using HomeCycle.Application.DTOs.Responses.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Moderators
{
    public interface IModeratorService
    {
        Task<Result<string>> ReviewBusinessProfileAsync(
            Guid moderatorId,
            ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<BusinessRegistrationDetailDto>> GetBusinessProfileDetailForModeratorAsync(
             Guid businessProfileId,
             CancellationToken cancellationToken = default);

        Task<Result<List<PendingBusinessProfileSummaryDto>>> GetPendingBusinessProfilesAsync(
            string? keyword,
            CancellationToken cancellationToken = default);

        Task<Result<List<PendingPersonalVerificationSummaryDto>>>GetPendingPersonalVerificationsAsync(string? keyword,
        CancellationToken cancellationToken = default);

        Task<Result<PersonalIdentityVerificationDetailDto>>GetPersonalVerificationDetailAsync(Guid personalProfileId, CancellationToken cancellationToken = default);

        Task<Result<string>> ReviewPersonalIdentityAsync(
            Guid moderatorId,
            Guid personalProfileId,
            ReviewPersonalIdentityRequest request,
            CancellationToken cancellationToken = default);
    }
}
