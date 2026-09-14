using HomeCycle.Application.DTOs.Requests.Offers;
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

        Task<PagedResult<offer>> GetSentAsync(Guid senderId, OfferSearchRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<offer>> GetReceivedAsync(Guid receiverId, OfferSearchRequest request, CancellationToken cancellationToken = default);
        Task<offer?> GetByIdForUpdateAsync(Guid offerId, CancellationToken cancellationToken);

        Task<bool> ExistsPendingByPostAndSenderAsync(Guid postId, Guid senderId, Guid receiverId, Guid? buyPostId, CancellationToken cancellationToken = default);

        Task ClosePendingByPostAsync(Guid postId, HomeCycle.Domain.Enums.OfferStatus status, CancellationToken cancellationToken = default);
        Task AddAsync(offer entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(offer entity, CancellationToken cancellationToken = default);
    }
}
