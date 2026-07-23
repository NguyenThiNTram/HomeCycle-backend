using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Users
{
    public interface IUserService
    {
        Task<Result<PublicUserProfileResponse>> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
