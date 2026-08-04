using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IMessageRepository
    {
        Task<message?> GetPendingProposalByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);
        Task<message?> GetPendingProposalForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default);
        Task<message?> GetByIdForUpdateAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task<PagedResult<message>> GetByNegotiationIdAsync(Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task AddAsync(message entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(message entity, CancellationToken cancellationToken = default);
    }
}
