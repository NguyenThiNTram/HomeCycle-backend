using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Orders;
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
        private readonly IOrderRepository _orderRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        public ChatHub(
            INegotiationRepository negotiationRepository,
            IConversationRepository conversationRepository,
            IOrderRepository orderRepository,
            IAgreementFormRepository agreementRepository)
        {
            _negotiationRepository = negotiationRepository;
            _conversationRepository = conversationRepository;
            _orderRepository = orderRepository;
            _agreementRepository = agreementRepository;
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


        public async Task JoinOrder(Guid orderId)
        {
            var userId = GetCurrentUserId();

            var order = await _orderRepository.GetByIdAsync(orderId, Context.ConnectionAborted);

            if (order == null)
                throw new HubException("NOT_FOUND");

            var agreement = await _agreementRepository.GetByIdAsync(
                order.AgreementId,
                Context.ConnectionAborted);

            if (agreement == null)
                throw new HubException("NOT_FOUND");

            if (agreement.BuyerId != userId && agreement.SellerId != userId)
                throw new HubException("FORBIDDEN");

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatGroupName.ForOrder(orderId),
                Context.ConnectionAborted);
        }

        public Task LeaveOrder(Guid orderId)
        {
            return Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                ChatGroupName.ForOrder(orderId),
                Context.ConnectionAborted);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("UNAUTHORIZED");

            return userId;
        }
    }
}
