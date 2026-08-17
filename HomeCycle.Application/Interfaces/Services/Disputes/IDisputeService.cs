using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Disputes
{
    public interface IDisputeService
    {
        Task<Result<CreateDisputeResponse>>
            CreateAsync(
                Guid senderId,
                CreateDisputeRequest request,
                CancellationToken cancellationToken = default);

        Task<Result<DisputeDetailResponse>>
            GetDetailForUserAsync(
                Guid disputeId,
                Guid currentUserId,
                CancellationToken cancellationToken = default);

        Task<Result<PagedResult<DisputeListItemResponse>>>
            GetAllForModeratorAsync(
                DisputeSearchRequest request,
                CancellationToken cancellationToken = default);

        Task<Result<DisputeDetailResponse>>
            GetDetailForModeratorAsync(
                Guid disputeId,
                CancellationToken cancellationToken = default);
    }
}
