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
        Task<Result<PagedResult<DisputeListItemResponse>>> GetForUserAsync(
            Guid currentUserId,
            DisputeSearchRequest request,
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
                Guid moderatorId,
                CancellationToken cancellationToken = default);

        Task<Result<ClaimDisputeResponse>> ClaimForModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            CancellationToken cancellationToken = default);

        Task<Result<DisputeDecisionResponse>> ResolveByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<DisputeDecisionResponse>> RejectByModeratorAsync(
            Guid disputeId,
            Guid moderatorId,
            DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<CloseDisputeResponse>> CloseDisputeAsync(
            Guid disputeId,
            Guid currentUserId,
            CancellationToken cancellationToken = default);
    }
}
