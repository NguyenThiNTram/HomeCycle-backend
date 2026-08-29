using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
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
        Task<Result<OfferResponse>> CreateAsync(Guid userId, CreateOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferResponse>> UpdateAsync(Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default);

        // Sender hủy Offer ban đầu khi còn Pending
        Task<Result<OfferResponse>> CancelAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        // Receiver từ chối Offer ban đầu
        Task<Result<OfferResponse>> RejectAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        // Receiver chấp nhận nguyên đề nghị && Tạo Negotiation ở trạng thái Agreed
        Task<Result<AcceptOfferResponse>> AcceptAsync(Guid userId, Guid offerId, AcceptOfferRequest request, CancellationToken cancellationToken = default);

        // Receiver thay đổi giá or số lượng && Tạo Negotiation ở trạng thái Open
        Task<Result<NegotiationResponse>> CounterInitialOfferAsync(Guid userId, Guid offerId, CounterInitialOfferRequest request, CancellationToken cancellationToken = default);

        Task<Result<OfferDetailResponse>> GetByIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferListItem>>> GetSentAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OfferListItem>>> GetReceivedAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);
    }
}
