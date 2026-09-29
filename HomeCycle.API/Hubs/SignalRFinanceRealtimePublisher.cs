using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    public sealed class SignalRFinanceRealtimePublisher : IFinanceRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalRFinanceRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishToUserAsync(
            Guid userId,
            FinanceUpdatedResponse response,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .User(userId.ToString())
                .FinanceUpdated(response)
                .WaitAsync(ct);
        }

        public Task PublishToFinanceGroupAsync(
            FinanceUpdatedResponse response,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.Finance)
                .FinanceUpdated(response)
                .WaitAsync(ct);
        }
    }
}
