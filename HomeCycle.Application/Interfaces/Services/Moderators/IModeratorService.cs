using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Moderators;
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
            Guid profileId,
            ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken = default);
    }
}
