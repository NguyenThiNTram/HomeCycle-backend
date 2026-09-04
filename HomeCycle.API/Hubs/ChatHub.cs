using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub<IChatClient>
    {
        public const string Route = "/hubs/chat";

        private readonly INegotiationRepository _negotiationRepository;
        private readonly IConversationRepository _conversationRepository;

        public ChatHub(
            INegotiationRepository negotiationRepository,
            IConversationRepository conversationRepository)
        {
            _negotiationRepository = negotiationRepository;
            _conversationRepository = conversationRepository;
        }

        public async Task JoinNegotiation(Guid negotiationId)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("UNAUTHORIZED");

            var negotiation = await _negotiationRepository.GetByIdAsync(negotiationId, Context.ConnectionAborted);
            if (negotiation is null || !NegotiationAccess.IsParticipant(negotiation, userId))
            {
                throw new HubException("FORBIDDEN");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroupName.ForNegotiation(negotiationId), Context.ConnectionAborted);
        }

        public Task LeaveNegotiation(Guid negotiationId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroupName.ForNegotiation(negotiationId), Context.ConnectionAborted);
        }

        public async Task JoinConversation(Guid conversationId)
        {
            var userId = GetCurrentUserId();

            var isParticipant = await _conversationRepository.IsParticipantAsync(conversationId, userId, Context.ConnectionAborted);

            if (!isParticipant)
                throw new HubException("FORBIDDEN");

            await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroupName.ForConversation(conversationId), Context.ConnectionAborted);
        }

        public Task LeaveConversation(Guid conversationId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroupName.ForConversation(conversationId), Context.ConnectionAborted);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("UNAUTHORIZED");

            return userId;
        }
    }
}
