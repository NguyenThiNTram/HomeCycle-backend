using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Negotiations;
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

        Task<Result<OfferResponse>> UpdateAsync(Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> CancelAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> RejectAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<AcceptOfferResponse>> AcceptAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> CounterInitialOfferAsync(Guid userId, Guid offerId, CounterInitialOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> SendNegotiationCounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> AcceptNegotiationAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> RejectNegotiationProposalAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> GetByIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferListItem>>> GetSentAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferListItem>>> GetReceivedAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> GetNegotiationByIdAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationResponse>> GetNegotiationByOfferIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<MessageResponse>>> GetNegotiationMessagesAsync(Guid userId, Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default);
    }
}
