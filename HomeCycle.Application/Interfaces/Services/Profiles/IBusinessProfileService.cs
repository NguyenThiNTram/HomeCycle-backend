using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Profiles
{
    public interface IBusinessProfileService
    {
        Task<Result<string>> SubmitBusinessProfileAsync(
            Guid userId,
            SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<BusinessRegistrationDetailDto>> GetRegistrationDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
        Task<Result> SaveProcurementPreferenceAsync(Guid userId, SubmitBusinessSurveyRequest request, CancellationToken cancellationToken);
        Task<Result<BusinessSurveyDetailResponse>> GetProcurementPreferenceAsync(Guid userId, CancellationToken cancellationToken);
        Task<Result<BusinessOnboardingStatus>> GetOnboardingStatusAsync(Guid userId, CancellationToken cancellationToken);

        Task<Result<BusinessProfileDetailDto>> GetBusinessProfileAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result> UpdateUsernameAsync(Guid userId, UpdateUsernameRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdatePhoneNumberAsync(Guid userId, UpdatePhoneNumberRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateBankAccountAsync(Guid userId, UpdateBankAccountRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateApprovedBusinessProfileAsync(Guid userId, UpdateApprovedBusinessProfileRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateBusinessDocumentsAsync(Guid userId, UpdateBusinessDocumentsRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateBusinessServiceAreasAsync(Guid userId, UpdateBusinessServiceAreasRequest request, CancellationToken cancellationToken = default);

    }
}
