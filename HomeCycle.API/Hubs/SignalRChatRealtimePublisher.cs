using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using Microsoft.AspNetCore.SignalR;
using System.Linq;

namespace HomeCycle.API.Hubs
{
    public sealed class SignalRChatRealtimePublisher : IChatRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalRChatRealtimePublisher(
        IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishMessageCreatedAsync(Guid negotiationId, MessageResponse message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForNegotiation(negotiationId))
                .MessageCreated(message);
        }

        public Task PublishMessageUpdatedAsync(Guid negotiationId, MessageResponse message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForNegotiation(negotiationId))
                .MessageUpdated(message);
        }

        public Task PublishMessagesReadAsync( Guid negotiationId, MessagesReadResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForNegotiation(negotiationId))
                .MessagesRead(response);
        }

        public Task PublishConversationUpdatedAsync(IReadOnlyList<Guid> userIds, ConversationUpdatedResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Users(userIds.Select(u => u.ToString()))
                .ConversationUpdated(response);
        }
    }
}
