using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Profiles;
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

        Task<Result<BusinessProfileStatus?>> GetProfileStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
