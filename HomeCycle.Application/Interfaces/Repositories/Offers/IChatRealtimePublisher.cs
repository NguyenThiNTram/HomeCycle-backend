using HomeCycle.Application.DTOs.Responses.Negotiations;
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

        Task PublishConversationUpdatedAsync(IReadOnlyList<Guid> userIds, ConversationUpdatedResponse response, CancellationToken cancellationToken = default);
    }
}
