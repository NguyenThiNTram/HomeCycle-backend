using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Profiles;
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
        Task<Result> SaveProcurementPreferenceAsync(Guid userId, SubmitBusinessSurveyRequest request, CancellationToken cancellationToken);

        Task<Result> SaveProcurementPreferenceAsync(Guid userId, SubmitBusinessSurveyRequest request, CancellationToken cancellationToken);
        Task<Result<BusinessSurveyDetailResponse>> GetProcurementPreferenceAsync(Guid userId, CancellationToken cancellationToken);
        Task<Result<BusinessOnboardingStatus>> GetOnboardingStatusAsync(Guid userId, CancellationToken cancellationToken);


    }
}
