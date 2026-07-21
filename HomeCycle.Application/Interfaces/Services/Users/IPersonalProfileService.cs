using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Users
{
    public interface IPersonalProfileService
    {
        Task<Result<PersonalProfileResponse>> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> UpdateProfileAsync(Guid userId, UpdatePersonalProfileRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateIdentityAsync(Guid userId, UpdateIdCardRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateBankAsync(Guid userId, UpdateBankAccountRequest request, CancellationToken cancellationToken = default);

        Task<Result<string>> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest file, CancellationToken cancellationToken = default);

        //Task<Result> DeleteAvatarAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
