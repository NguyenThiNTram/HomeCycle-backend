using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Offers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Offers
{
    public interface IOfferService
    {
        Task<Result<OfferResponse>> CreateOfferAsync(Guid userId, CreateOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> GetByIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferResponse>>> GetSentAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferResponse>>> GetReceivedAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> UpdateAsync(Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> CancelAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> AcceptAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> RejectAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);
    }
}
