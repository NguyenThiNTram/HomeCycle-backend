using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface INegotiationRepository
    {
        Task<negotiation?> GetByOfferIdAsync(Guid offerId, CancellationToken cancellationToken = default);

        Task<negotiation?> GetByIdAsync(Guid negotiationId, CancellationToken cancellationToken = default);
        Task<negotiation?> GetByIdForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default);

        Task<PagedResult<negotiation>> GetByParticipantAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task AddAsync(negotiation entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(negotiation entity, CancellationToken cancellationToken = default);
    }
}
