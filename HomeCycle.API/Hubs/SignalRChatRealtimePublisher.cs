using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
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
                .MessageCreated(message)
                .WaitAsync(cancellationToken);
        }

        public Task PublishMessageUpdatedAsync(Guid negotiationId, MessageResponse message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForNegotiation(negotiationId))
                .MessageUpdated(message)
                .WaitAsync(cancellationToken);
        }

        public Task PublishMessagesReadAsync( Guid negotiationId, MessagesReadResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForNegotiation(negotiationId))
                .MessagesRead(response)
                .WaitAsync(cancellationToken);
        }

        public Task PublishConversationUpdatedAsync(IReadOnlyList<Guid> userIds, ConversationUpdatedResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Users(userIds.Select(u => u.ToString()))
                .ConversationUpdated(response)
                .WaitAsync(cancellationToken);
        }

        public Task PublishOfferCreatedAsync(Guid receiverId, OfferResponse offer, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients
                .User(receiverId.ToString())
                .OfferCreated(offer)
                .WaitAsync(cancellationToken);
        }

        public Task PublishOfferUpdatedAsync(IReadOnlyList<Guid> userIds, OfferResponse offer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _hubContext.Clients
                .Users(userIds.Select(u => u.ToString()))
                .OfferUpdated(offer)
                .WaitAsync(cancellationToken);
        }
    }
}
