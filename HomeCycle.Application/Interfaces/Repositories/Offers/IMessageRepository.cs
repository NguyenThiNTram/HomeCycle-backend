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
        Task<message?> GetPendingCounterOfferByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);

        Task AddAsync(message entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(message entity, CancellationToken cancellationToken = default);
    }
}
