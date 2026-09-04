using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IChatRealtimePublisher
    {
        Task PublishMessageCreatedAsync(Guid negotiationId, MessageResponse message, CancellationToken cancellationToken = default);

        Task PublishMessageUpdatedAsync(Guid negotiationId, MessageResponse message, CancellationToken cancellationToken = default);

        Task PublishMessagesReadAsync(Guid negotiationId, MessagesReadResponse response, CancellationToken cancellationToken = default);

        Task PublishConversationMessageCreatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken = default);

        Task PublishConversationMessageUpdatedAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken = default);

        Task PublishConversationMessagesReadAsync(Guid conversationId, ConversationMessagesReadResponse response, CancellationToken cancellationToken = default);

        Task PublishConversationUpdatedAsync(IReadOnlyList<Guid> userIds, ConversationUpdatedResponse response, CancellationToken cancellationToken = default);

        Task PublishOfferCreatedAsync(Guid receiverId, OfferResponse offer, CancellationToken cancellationToken = default);

        Task PublishOfferUpdatedAsync(IReadOnlyList<Guid> userIds, OfferResponse offer, CancellationToken cancellationToken = default);
    }
}
