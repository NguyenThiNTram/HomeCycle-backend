using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Negotiates
{
    public interface IConversationService
    {
        //FE reload
        Task<Result<ConversationListItemResponse>> GetByIdAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
        //Danh sách inbox
        Task<Result<PagedResult<ConversationListItemResponse>>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);
        //Timeline tổng hợp Message
        Task<Result<PagedResult<MessageResponse>>> GetTimelineAsync(Guid userId, Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default);
        //Danh sách Negotiation của Conversation
        Task<Result<PagedResult<NegotiationListItemResponse>>> GetNegotiationsAsync(Guid userId, Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result> MarkAsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    }
}
