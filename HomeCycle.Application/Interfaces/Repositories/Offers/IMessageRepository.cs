using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IMessageRepository
    {
        Task<message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task<message?> GetByIdForUpdateAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task<message?> GetByClientMessageIdAsync(Guid negotiationId, Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default);

        Task<message?> GetPendingProposalByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);

        Task<message?> GetPendingProposalForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default);

        Task<PagedResult<message>> GetByNegotiationIdAsync(Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task AddAsync(message entity, CancellationToken cancellationToken = default);

        Task<bool> TryUpdateProposalStatusAsync(Guid messageId, MessageOfferStatus expectedStatus, MessageOfferStatus newStatus, DateTime updatedAt, CancellationToken cancellationToken = default);

        Task<int> MarkAsReadAsync(Guid negotiationId, Guid readerId, DateTime readAt, CancellationToken cancellationToken = default);

        Task<int> CountUnreadByNegotiationForUserAsync(Guid negotiationId, Guid userId, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, int>> GetUnreadCountsByNegotiationAsync(Guid negotiationId, Guid buyerId, Guid sellerId, CancellationToken cancellationToken = default);

        //conversation messages
        Task<message?> GetByClientMessageIdInConversationAsync(Guid conversationId, Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default);

        Task<PagedResult<message>> GetByConversationIdAsync(Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<int> MarkConversationAsReadAsync(Guid conversationId, Guid readerId, DateTime readAt,  CancellationToken cancellationToken = default);

        Task<int> CountUnreadByConversationForUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, int>> GetUnreadCountsByConversationsAsync(Guid userId, IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, message>> GetLatestByConversationsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, int>> GetUnreadCountsByConversationAsync(Guid conversationId,  Guid userOneId, Guid userTwoId, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, UnreadCountResult>> GetUnreadCountsDetailAsync(Guid conversationId, Guid userOneId, Guid userTwoId, CancellationToken cancellationToken = default);
    }
}
