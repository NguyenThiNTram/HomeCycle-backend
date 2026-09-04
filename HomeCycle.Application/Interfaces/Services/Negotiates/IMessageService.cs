using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Negotiates
{
    public interface IMessageService
    {
        Task<Result<MessageResponse>> SendAsync(Guid userId, Guid negotiationId, SendMessageRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<MessageResponse>>> GetHistoryAsync(
            Guid userId,
            Guid negotiationId,
            PaginationRequest request,
            CancellationToken cancellationToken = default);

        Task<Result> MarkAsReadAsync(
            Guid userId,
            Guid negotiationId,
            CancellationToken cancellationToken = default);

        Task<Result> MarkConversationAsReadAsync(
            Guid userId,
            Guid conversationId,
            CancellationToken cancellationToken = default);
    }
}
