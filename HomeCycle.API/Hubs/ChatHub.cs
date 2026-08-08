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

        public ChatHub(
            INegotiationRepository negotiationRepository)
        {
            _negotiationRepository = negotiationRepository;
        }

        public async Task JoinNegotiation(Guid negotiationId)
        {
            if (!Guid.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("UNAUTHORIZED");

            var negotiation =
                await _negotiationRepository.GetByIdAsync(
                    negotiationId,
                    Context.ConnectionAborted);

            // Trả cùng một lỗi để không tiết lộ negotiation có tồn tại hay không.
            if (negotiation is null ||
                !NegotiationAccess.IsParticipant(negotiation, userId))
            {
                throw new HubException("FORBIDDEN");
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatGroupName.ForNegotiation(negotiationId),
                Context.ConnectionAborted);
        }

        public Task LeaveNegotiation(Guid negotiationId)
        {
            return Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                ChatGroupName.ForNegotiation(negotiationId),
                Context.ConnectionAborted);
        }
    }
}
