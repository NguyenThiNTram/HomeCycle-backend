using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Negotiates
{
    public interface INegotiationService
    {
        Task<Result<NegotiationDetailResponse>> GetByIdAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationDetailResponse>> GetByOfferIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<NegotiationListItemResponse>>> GetMyNegotiationsAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        //Task<Result<NegotiationProposalResponse>> CounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default);

        //Task<Result<NegotiationResponse>> AcceptProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default);

        //Task<Result<NegotiationProposalResponse>> RejectProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId,
        //    CancellationToken cancellationToken = default);

        //// Buyer hoặc Seller đều được hủy khi Open
        //Task<Result<NegotiationResponse>> CancelAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationActionResponse>> CounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default);

        Task<Result<NegotiationActionResponse>> AcceptProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationActionResponse>> RejectProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default);

        Task<Result<NegotiationActionResponse>> CancelAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default);

        // Chỉ gọi nội bộ sau khi Agreement/Order hoàn tất
        Task<Result> CloseAsync(Guid negotiationId, CancellationToken cancellationToken = default);
    }
}
