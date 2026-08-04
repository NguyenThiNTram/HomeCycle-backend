using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IOfferRepository
    {
        Task<offer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken = default);

        Task<PagedResult<offer>> GetSentAsync(Guid senderId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<offer>> GetReceivedAsync(Guid receiverId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<bool> ExistsPendingByPostAndSenderAsync(Guid postId, Guid senderId, CancellationToken cancellationToken = default);

        Task AddAsync(offer entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(offer entity, CancellationToken cancellationToken = default);
    }
}
