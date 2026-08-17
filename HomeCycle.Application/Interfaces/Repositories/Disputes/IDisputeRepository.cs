using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Disputes
{
    public interface IDisputeRepository
    {
        Task AddAsync(
            dispute dispute,
            CancellationToken cancellationToken = default);

        Task<dispute?> GetByIdAsync(
            Guid disputeId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsActiveAsync(
            DisputeTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<DisputeListItemResponse>>
            GetPagedForModeratorAsync(
                DisputeSearchRequest request,
                CancellationToken cancellationToken = default);
    }
}
